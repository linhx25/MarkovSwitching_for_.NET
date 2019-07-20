using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    /// <summary>
    /// Class to contain model results
    /// </summary>
    public class Results
    {
        public Model model;
        public double[] _params;
        public int k_constant;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model">class instance
        /// the previously specified model instance</param>
        /// <param name="_params">array
        /// parameter estimates from the fit model</param>
        /// <param name="kwd"></param>
        public Results(Model model,double[] _params,Dictionary<string,object> kwd=null)
        {
            this.model = model;
            this._params = _params;
            this.k_constant = model.k_constant;
        }

        public double[] predict(double[] exog=null,bool transform=true)
        {
            /*
             * Call self.model.predict with self.params as the first argument.
             * 
             * Parameters
             * ----------
             * exog : array-like, optional
             * The values for which you want to predict.
             * transform : bool, optional
             * If the model was fit via a formula, do you want to pass
             * exog through the formula. Default is True. E.g., if you fit
             * a model y ~ log(x1) + log(x2), and transform is True, then
             * you can pass a data structure that contains x1 and x2 in
             * their original form. Otherwise, you'd need to log the data
             * first.
             * args, kwargs :
             * Some models can take additional arguments or keywords, see the
             * predict method of the model for the details.
             * 
             * Returns
             * -------
             * prediction : ndarray, pandas.Series or pandas.DataFrame
             * See self.model.predict
             */

            return null;
        }

        public void summary()
        {
        }
    }
    
}
