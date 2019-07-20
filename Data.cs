using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumpyBase;

namespace StatsModels
{
    public class Data
    {
        public string[] _param_names = null;
        public int missing_row_idx;
        public double[] endog;
        public numpy<double> endog_np;
        public Array exog;
        public numpy<double> exog_np;
        public double[] orig_endog;
        public Array orig_exog;
        public Dictionary<string, object> _dict_;
        public bool hasconst;
        public string hasconst_fl;
        public int k_constant;
        public numpy<int> const_idx;
        public string const_idx_fl;

        public Data(double[] endog, Array exog = null, string missing = null,bool hasconst =false, string flag = null)
        {
            endog_np = new numpy<double>(endog);
            exog_np = new numpy<double>(exog);
        }
        public Data(double[] endog,int nan_mask, Array exog = null,int method = 1) 
        {
            endog_np = new numpy<double>(endog);
            exog_np = new numpy<double>(exog);
        }
        public Data(double[] endog, Array exog = null, string missing = null, Dictionary<string, object> kwargs = null)
        {
            endog_np = new numpy<double>(endog);
            exog_np = new numpy<double>(exog); 
        }

        //....
        public Dictionary<string, object> _getstate_() 
        {
            var d = new Dictionary<string, object>(_dict_);
            return d;
        }
        //....
        public void _setstate(Dictionary<string, object> d)
        { 
        }

        public void _handle_constant(string flag,bool hasconst)
        { 
            if(flag!=null)
                if (hasconst)
                {
                    k_constant = 1;
                    const_idx_fl = null;
                }
                else
                {
                    k_constant = 0;
                    const_idx_fl = null;
                }
            else if (exog != null)
            {
                const_idx_fl = null;
                k_constant = 0;
            }
            else
            {
                // detect where the constant is
                var check_implicit = false;
                //....
                var const_idx_tmp = new numpy<int>().squeeze();

                k_constant = const_idx.Length;

                if (k_constant == 1)
                    if (exog_np.sum() != 0)//....
                        const_idx = const_idx_tmp;
                    else
                        // we only have a zero column and no other constant
                        check_implicit = true;
                else if (k_constant > 1)
                {
                }
                else if (k_constant == 0)
                    check_implicit = true;

                if (check_implicit)
                { 
                }
            }
        }

        public string[] param_names
        {
            get { return this._param_names; } 
        }
    }
}
