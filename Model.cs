using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class Model
    {
        // 1-d endogenous response variable. The dependent variable
        public double[] endog;
        // A nobs x k array where `nobs` is the number of observations and `k`
        // is the number of regressors
        public Array exog;

        public int k_constant;
        public string missing;
        public bool hasconst;
        public List<string> _data_attr;
        public List<string> _init_keys;
        //public Array data;
        

        public Model(double[] endog,Array exog=null,Dictionary<string,object> kwargs=null)
        {
            this.endog = endog;
            this.exog = exog;
            missing = kwargs.ContainsKey("missing") ? (string)kwargs["missing"] : "none";
            int flag;// for temporary use
            if (kwargs.ContainsKey("hasconst"))
            { 
                hasconst = (bool)kwargs["hasconst"]; flag = 1; 
            }
            else
                flag = 0;
            //self.k_constant = self.data.k_constant
            //self.exog = self.data.exog
            //self.endog = self.data.endog

            _data_attr.AddRange(new string[] { "exog", "endog", "data.exog", "data.endog" });
            if (!kwargs.ContainsKey("formula"))
                _data_attr.AddRange(new string[] { "data.orig_endog","data.orig_exog" });
            _init_keys = kwargs.Keys.ToList();
            if (flag == 0)
                _init_keys.Add("hasconst");
        }

        public Model(double[] endog,string formula, Array exog = null, Dictionary<string, object> kwargs = null) 
        {
            /*
             Create a Model from a formula and dataframe.

            Parameters
            ----------
            formula : str or generic Formula object
                The formula specifying the model
            data : array-like
                The data for the model. See Notes.
            subset : array-like
                An array-like object of booleans, integers, or index values that
                indicate the subset of df to use in the model. Assumes df is a
                `pandas.DataFrame`
            drop_cols : array-like
                Columns to drop from the design matrix.  Cannot be used to
                drop terms involving categoricals.
            args : extra arguments
                These are passed to the model
            kwargs : extra keyword arguments
                These are passed to the model with one exception. The
                ``eval_env`` keyword is passed to patsy. It can be either a
                :class:`patsy:patsy.EvalEnvironment` object or an integer
                indicating the depth of the namespace to use. For example, the
                default ``eval_env=0`` uses the calling namespace. If you wish
                to use a "clean" environment set ``eval_env=-1``.

            Returns
            -------
            model : Model instance

            Notes
            ------
            data must define __getitem__ with the keys in the formula terms
            args and kwargs are passed on to the model instantiation. E.g.,
            a numpy structured or rec array, a dictionary, or a pandas DataFrame.
            a numpy structured or rec array, a dictionary, or a pandas DataFrame.*/
        }

        public Dictionary<string, object> _get_init_kewds()
        {
            // return dictionary with extra keys used in model.__init__
            var kwds = new Dictionary<string, object>();
            foreach (var key in this._init_keys)
            {
                if (this.GetType().GetMember((string)key) != null)
                    kwds.Add((string)key, this.GetType().GetMember((string)key));
                else
                    kwds.Add((string)key, null);
            }
            return kwds;
        }

        // Names of endogenous variablesNames of endogenous variables
        public string endog_name
        {
            get { return null; }
        }

        // Names of exogenous variables
        public string exog_names
        {
            get { return null; }
        }

        // Fit a model to data
        public void fit()
        {
            throw new NotImplementedException();
        }

        public void predict(double[] exog=null)
        {
            // After a model has been fit predict returns the fitted values.                
            // This is a placeholder intended to be overwritten by individual models
            throw new NotImplementedException();
        }

    }
}
