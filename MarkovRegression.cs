using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class MarkovRegression:MarkovSwitching
    {
        public MarkovRegression(double[] endog, int k_regimes,string trend="c",double[] exog=null,
            int order = 0,double[] exog_tvtp=null,
            bool switching_trend = true,
            bool switching_exog = true,
            bool switching_variance=false,
            object dates = null,
            string freq = null,
            string missing = "none"):
            base(endog,k_regimes,order,exog_tvtp,exog,dates,freq,missing)
        {
        }
    }
}
