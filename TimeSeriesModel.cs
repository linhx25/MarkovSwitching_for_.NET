using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class TimeSeriesModel : LikelihoodModel
    {
        public TimeSeriesModel(double[] endog, double[] exog = null, Dictionary<string, object> kwargs = null)
            : base(endog, exog, kwargs)
        {
        }
    }

    public class TimeSeriesModelResults : LikelihoodModelResults
    {

        public TimeSeriesModelResults(LikelihoodModel model, double[] _params, double[,] normalized_cov_params, double scale = 1.0)
            : base(model, _params, normalized_cov_params, scale) 
        {

        }
    
    
    }
}
