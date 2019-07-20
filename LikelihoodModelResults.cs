using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class LikelihoodModelResults:Results
    {
        double[,] normalized_cov_params;
        double scale;

        public LikelihoodModelResults(Model model,double[] ests,double[,] normalized_cov_params=null,double scale=1.0):base(model,ests)
        {
            this.normalized_cov_params = normalized_cov_params;
            this.scale = scale;

        }
    }
}
