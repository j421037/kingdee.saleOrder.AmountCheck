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
    /// 客户应收 = 销售应收 - 收款 + 收款退款 - 应收核销    //暂定
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
            e.CancelMessage = string.Format("客户id：{0}, 整单金额: {1}, 客户额度：{2}, 客户全部应收： {3}", cust_id, allAmount, LoaCredit(cust_id), $"{LoadCustAR(cust_id):N}");
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

        /// <summary>
        /// 读取客户应收款汇总
        /// </summary>
        /// <param name="cust_id"></param>
        /// <returns></returns>
        private decimal LoadCustAR(string cust_id)
        {
            decimal total = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT sum(FallAmountFor) as amount from t_AR_receivable WHERE fcustomerID = {0} and Fdocumentstatus = 'C' ", cust_id);

            using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sb.ToString()))
            {
                if (reader.Read())
                {
                    string amount = reader["amount"].ToString();

                    if (!string.IsNullOrEmpty(amount))
                    {
                        total = Convert.ToDecimal(amount);
                    }
                }
            }
            return total;
        }


        /// <summary>
        /// 读取客户收款
        /// </summary>
        /// <param name="cust_id"></param>
        /// <returns></returns>
        private decimal LoadCustReceivables(string cust_id)
        {
            decimal total = 0;
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("SELECT SUM(FRECEIVEAMOUNT) AS amount FROM T_AR_RECEIVEBILL fcontactunit = {0} and Fdocumentstatus = 'C'", cust_id);

            using (IDataReader reader = DBUtils.ExecuteReader(this.Context, sb.ToString()))
            {
                if (reader.Read())
                {
                    string amount = reader["amount"].ToString();

                    if (!string.IsNullOrEmpty(amount))
                    {
                        total = Convert.ToDecimal(amount);
                    }
                }
            }

            return total;
        }
    }
}
