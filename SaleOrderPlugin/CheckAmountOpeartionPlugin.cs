using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;

namespace YHT.K3.SCM.App.Sal.ServicePlugIn
{
    /// <summary>
    /// 销售订单金额设计插件
    /// </summary>
    /// 
    //[System.ComponentModel.Description("销售订单金额审计插件")]
    [Kingdee.BOS.Util.HotUpdate]
    public class CheckAmountOpeartionPlugin: AbstractOperationServicePlugIn
    {
        private const decimal DEFAULT_CREDIT = 500000;

        /// <summary>
        /// 执行订单保存操作之前
        /// </summary>
        /// <param name="e"></param>
        /// 客户id = e.SelectedRows[0][DataEntity][custId_Id]
        /// 整单金额 = e.SelectedRows[0][DataEntity][SaleOrderFinance][0][BillAllAmount]
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            string cust_id = null;
            decimal allAmount = 0;

            foreach (ExtendedDataEntity extended in e.SelectedRows)
            {
                DynamicObject entity = extended.DataEntity;
                DynamicObjectCollection finance = entity["SaleOrderFinance"] as DynamicObjectCollection;
                cust_id = entity["CustId_Id"].ToString();

                foreach (DynamicObject fin in finance)
                {
                    allAmount = Convert.ToDecimal(fin["BillAllAmount"]);
                }
            }

            e.Cancel = true;
            e.CancelMessage = string.Format("客户id：{0}, 整单金额: {1}, 客户额度：{2}", cust_id, allAmount, LoaCredit(cust_id));
        }

        /// <summary>
        /// 读取客户额度
        /// </summary>
        /// <param name="cust_id"></param>
        /// <returns></returns>
        private decimal LoaCredit(string cust_id)
        {
            decimal credit = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT * FROM T_BD_CUSTOMER WHERE FCUSTID = {0}", cust_id);

            using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sb.ToString()))
            {
                if (reader.Read())
                {
                    string col_val = reader["F_M_CREDIT"].ToString();

                    if (!string.IsNullOrEmpty(col_val)) {
                        credit = Convert.ToDecimal(col_val);
                    }
                }
            }

            return credit > 0 ? credit : DEFAULT_CREDIT ;
        }
    }
}
