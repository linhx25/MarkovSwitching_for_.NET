using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class MarkovAutoregression:MarkovRegression
    {
        public MarkovAutoregression(double[] endog, int k_regimes,int order,string trend="c",double[] exog = null,
            double[] exog_tvtp = null,
            bool switching_ar = true,
            bool switching_trend = true,
            bool switching_exog = false,
            bool switching_variance = false,
            object dates = null,
            string freq =null,
            string missing = "none"):
            base(endog,k_regimes,trend,exog,order,exog_tvtp,switching_trend,switching_exog,switching_variance,dates,freq,missing)
        {
        }
    }
}
