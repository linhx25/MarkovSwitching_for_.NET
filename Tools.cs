using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class Tools
    {
        public static Tuple<int, double[,]>
            prepare_exog(double[] exog)
        {
            var k_exog = 0;
            if (exog != null)
            {
                k_exog = exog.Length;
                var new_exog = new double[1, k_exog];
                exog.CopyTo(new_exog, 0);
                return Tuple.Create(k_exog, new_exog);
            }
            return Tuple.Create(k_exog,(double[,])null);
        }
    }
}
