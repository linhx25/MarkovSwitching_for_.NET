using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumpyBase;

namespace StatsModels
{
    public class MarkovSwitching_Prepare
    {
        public Tuple<int, double[,]> _prepare_exog(Array exog)
        {
            //prepare the exogenous variable array for model fitting

            //the number of exogenous variable
            int k_exog = 0;
            var exog_np = new numpy<double>(exog);
            if (exog != null)
            {
                if (exog_np.dim == 1)//extend the array to 2-dimensioanl array                
                    exog_np = exog_np["..."].reshape(exog_np.Shape[0], 1);
                k_exog = exog_np.Shape[1];
            }
            return Tuple.Create(k_exog, (double[,])exog_np.ToArray());
        }
        public numpy<double> _logistic(numpy<double> x)
        {
            // prepare exp(x) / (1 + np.exp(x)) in array x
            // Note that this is not a vectorized function

            var y = new numpy<double>();
            var np = new numpy<double>();//for convinience
            // np.exp(x) / (1 + np.exp(x))
            if (x.dim == 0)
                y = x.reshape(1, 1, 1);
            // np.exp(x[i]) / (1 + np.sum(np.exp(x[:])))
            else if (x.dim == 1)
                y = x.reshape(x.Length, 1, 1);
            // np.exp(x[i,t]) / (1 + np.sum(np.exp(x[:,t])))
            else if (x.dim == 2)
                y = x.reshape(x.Shape[0], 1, x.Shape[1]);
            // np.exp(x[i,j,t]) / (1 + np.sum(np.exp(x[:,j,t])))
            else if (x.dim == 3)
                y = x;
            else
                throw new NotImplementedException();
            //tmp = np.c_[np.zeros((y.shape[-1], y.shape[1], 1)), y.T].T
            var tmp_np = np.concatenate(
                np.zeros(y.Shape[y.Shape.Length - 1], y.Shape[1], 1), y.t(), 1).t();
            //evaluated = np.reshape(np.exp(y - logsumexp(tmp, axis=0)), x.shape)
            var evaluated_np = (y - tmp_np.exp().sum(0).log()).exp().reshape(x.Shape);

            return evaluated_np;//the shape should be checked
        }
        public numpy<double> _partials_logistic(numpy<double> x)
        {
            var tmp_np = this._logistic(x);
            var partials_np = new numpy<double>();//unchecked
            var np = new numpy<double>();//for conviniece
            // k
            if (tmp_np.dim == 0)
                return tmp_np - tmp_np.Square();

            // k * k
            else if (tmp_np.dim == 1)
                partials_np = np.diag(tmp_np - tmp_np.Square());//2-D
            // k * k * t
            else if (tmp_np.dim == 2)
            {
                for (var t = 0; t < tmp_np.Shape[1]; t++)
                    partials_np = np.concatenate(partials_np,
                        np.diag(tmp_np[":," + t.ToString()] - tmp_np[":," + t.ToString()].Square()));//1-D
                var shape = new int[] { tmp_np.Shape[1], tmp_np.Shape[0], tmp_np.Shape[0] };
                partials_np = partials_np.reshape(shape).transpose(1, 2, 0);
            }
            // k * k * j * t
            else
            {
                for (var j = 0; j < tmp_np.Shape[1]; j++)
                    for (var t = 0; t < tmp_np.Shape[2]; t++)
                        partials_np = np.concatenate(partials_np,
                            np.diag(tmp_np[":," + j.ToString() + "," + t.ToString()] - tmp_np[":," + j.ToString() + "," + t.ToString()].Square()));
                var shape = new int[] { tmp_np.Shape[1], tmp_np.Shape[2], tmp_np.Shape[0], tmp_np.Shape[0] };
                partials_np = partials_np.reshape(shape).transpose(2, 3, 0, 1);
            }

            foreach (var i in Enumerable.Range(0, tmp_np.Shape[0]))
            {
                foreach (var j in Enumerable.Range(0, i))
                {
                    partials_np[i.ToString() + "," + j.ToString() + ",..."] = -tmp_np[i.ToString() + ",..."] * tmp_np[j.ToString() + ",..."];
                    partials_np[j.ToString() + "," + i.ToString() + ",..."] = partials_np[i.ToString() + "," + j.ToString() + ",..."];
                }
            }
            return partials_np;// the return type not checked
        }

        /********************************************************************
         * Hamilton filter using pure Python
         * Parameters
         * ----------
         * initial_probabilities : array
         * Array of initial probabilities, shaped (k_regimes,).
         * regime_transition : array
         * Matrix of regime transition probabilities, shaped either
         * (k_regimes, k_regimes, 1) or if there are time-varying transition
         * probabilities (k_regimes, k_regimes, nobs).
         * conditional_likelihoods : array
         * Array of likelihoods conditional on the last `order+1` regimes,
         * shaped (k_regimes,)*(order + 1) + (nobs,).
         * 
         * Returns
         * -------
         * 
         * filtered_marginal_probabilities : array
         * Array containing Pr[S_t=s_t | Y_t] - the probability of being in each
         * regime conditional on time t information. Shaped (k_regimes, nobs).
         * predicted_joint_probabilities : array
         * Array containing Pr[S_t=s_t, ..., S_{t-order}=s_{t-order} | Y_{t-1}] -
         * the joint probability of the current and previous `order` periods
         * being in each combination of regimes conditional on time t-1
         * information. Shaped (k_regimes,) * (order + 1) + (nobs,).
         * joint_likelihoods : array
         * Array of likelihoods condition on time t information, shaped (nobs,).
         * filtered_joint_probabilities : array
         * Array containing Pr[S_t=s_t, ..., S_{t-order}=s_{t-order} | Y_{t}] -
         * the joint probability of the current and previous `order` periods
         * being in each combination of regimes conditional on time t
         * information. Shaped (k_regimes,) * (order + 1) + (nobs,).
         * * *********************************************************************/
        public Tuple<double[,], Array, double[], Array> py_hamilton_filter(
            double[] initial_probabilities,
            double[, ,] regime_transition,
            Array conditional_likelihoods)
        {
            var np = new numpy<double>();//for creating numpy object
            var regime_transition_np = new numpy<double>(regime_transition);
            var conditional_likelihoods_np = new numpy<double>(conditional_likelihoods);

            //Dimensions
            int k_regimes = initial_probabilities.Length;
            int nobs = conditional_likelihoods_np.Shape[conditional_likelihoods_np.Shape.Length - 1];
            int order = conditional_likelihoods_np.dim - 2;

            //initialize the shape of the probabilities array
            var _shape = new int[order + 2];
            _shape[order + 1] = nobs;
            for (var i = 0; i < order + 1; i++)
                _shape[i] = k_regimes;

            // Storage
            // Pr[S_t = s_t | Y_t]
            var filtered_marginal_probabilities_np = np.zeros(k_regimes, nobs);
            // Pr[S_t = s_t, ... S_{t-r} = s_{t-r} | Y_{t-1}]
            var predicted_joint_probabilities_np = np.zeros(_shape);
            // f(y_t | Y_{t-1})
            var joint_likelihoods_np = np.zeros(nobs);
            // Pr[S_t = s_t, ... S_{t-r} = s_{t-r} | Y_t]
            _shape[order + 1] = nobs + 1;
            var filtered_joint_probabilities_np = np.zeros(_shape);

            // Initial probabilities
            filtered_marginal_probabilities_np[":, 0"] = new numpy<double>(initial_probabilities);
            var tmp = new numpy<double>(initial_probabilities);

            var shape = new List<int>();
            shape.Add(k_regimes); shape.Add(k_regimes);
            for (int i = 0; i < order; i++)
            {
                string str = "...," + i.ToString();
                for (int j = 0; j < i + 1; j++)
                    shape.Add(1);
                tmp = regime_transition_np[str].reshape(shape.ToArray()) * tmp;
            }
            filtered_joint_probabilities_np["...,0"] = tmp;

            // Reshape regime_transition so we can use broadcasting
            var arr_tmp = new int[order - 1];
            for (int i = 0; i < order - 1; i++)
                arr_tmp[i] = 1;
            shape.AddRange(arr_tmp);
            shape.Add(regime_transition_np.Shape[regime_transition_np.Shape.Length - 1]);
            regime_transition_np = regime_transition_np.reshape(shape.ToArray());

            // Get appropriate subset of transition matrix
            if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1)
                regime_transition_np = regime_transition_np["...," + order.ToString() + ":"];

            // Hamilton filter iterations
            var transition_t = 0;
            for (int t = 0; t < nobs; t++)
            {
                if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1)
                    transition_t = t;

                // S_t, S_{t-1}, ..., S_{t-r} | t-1, stored at zero-indexed location t
                predicted_joint_probabilities_np["...," + t.ToString()] =
                    // S_t | S_{t-1}
                    regime_transition_np["...," + transition_t.ToString()] *
                    // S_{t-1}, S_{t-2}, ..., S_{t-r} | t-1
                    filtered_joint_probabilities_np["...," + t.ToString()].sum(-1);

                // f(y_t, S_t, ..., S_{t-r} | t-1)
                tmp = conditional_likelihoods_np["...," + t.ToString()] *
                    predicted_joint_probabilities_np["...," + t.ToString()];
                // f(y_t | t-1)
                joint_likelihoods_np[t] = tmp.sum();

                // S_t, S_{t-1}, ..., S_{t-r} | t, stored at index t+1
                filtered_joint_probabilities_np["...," + (t + 1).ToString()] =
                    tmp / joint_likelihoods_np[t];
            }

            // S_t | t
            filtered_marginal_probabilities_np = filtered_joint_probabilities_np["...,1:"];
            for (int i = 1; i < filtered_marginal_probabilities_np.dim - 1; i++)
                filtered_marginal_probabilities_np = filtered_marginal_probabilities_np.sum(-2);

            // check the type of array...
            return Tuple.Create<double[,], Array, double[], Array>(
                (double[,])filtered_marginal_probabilities_np.ToArray(),
                predicted_joint_probabilities_np.ToArray(),
                (double[])joint_likelihoods_np.ToArray(),
                filtered_joint_probabilities_np["...,1:"].ToArray());
        }
        // ....
        public Tuple<double[,], Array, double[], Array> cy_hamilton_filter(
            double[] initial_probabilities,
            double[, ,] regime_transition,
            Array conditional_likelihoods)
        {
            var np = new numpy<double>();//for creating numpy object
            var regime_transition_np = new numpy<double>(regime_transition);
            var conditional_likelihoods_np = new numpy<double>(conditional_likelihoods);

            //Dimensions
            int k_regimes = initial_probabilities.Length;
            int nobs = conditional_likelihoods_np.Shape[conditional_likelihoods_np.Shape.Length - 1];
            int order = conditional_likelihoods_np.dim - 2;

            //initialize the shape of the probabilities array
            var _shape = new int[order + 2];
            _shape[order + 1] = nobs;
            for (var i = 0; i < order + 1; i++)
                _shape[i] = k_regimes;

            // Storage
            // Pr[S_t = s_t | Y_t]
            var filtered_marginal_probabilities_np = np.zeros(k_regimes, nobs);
            // Pr[S_t = s_t, ... S_{t-r} = s_{t-r} | Y_{t-1}]
            var predicted_joint_probabilities_np = np.zeros(_shape);
            // f(y_t | Y_{t-1})
            var joint_likelihoods_np = np.zeros(nobs);
            // Pr[S_t = s_t, ... S_{t-r} = s_{t-r} | Y_t]
            _shape[order + 1] = nobs + 1;
            var filtered_joint_probabilities_np = np.zeros(_shape);

            // Initial probabilities
            filtered_marginal_probabilities_np[":, 0"] = new numpy<double>(initial_probabilities);
            var tmp = new numpy<double>(initial_probabilities);

            var shape = new List<int>();
            shape.Add(k_regimes); shape.Add(k_regimes);
            for (int i = 0; i < order; i++)
            {
                string str = "...," + i.ToString();
                for (int j = 0; j < i + 1; j++)
                    shape.Add(1);
                tmp = regime_transition_np[str].reshape(shape.ToArray()) * tmp;
            }
            filtered_joint_probabilities_np["...,0"] = tmp;

            // Get appropriate subset of transition matrix
            if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1)
                regime_transition_np = regime_transition_np["...," + order.ToString() + ":"];

            // Run Cython filter iterations....


            // S_t | t
            filtered_marginal_probabilities_np = filtered_joint_probabilities_np["...,1:"];
            for (int i = 1; i < filtered_marginal_probabilities_np.dim - 1; i++)
                filtered_marginal_probabilities_np = filtered_marginal_probabilities_np.sum(-2);

            // check the type of array....
            return Tuple.Create<double[,], Array, double[], Array>(
                (double[,])filtered_marginal_probabilities_np.ToArray(),
                predicted_joint_probabilities_np.ToArray(),
                (double[])joint_likelihoods_np.ToArray(),
                filtered_joint_probabilities_np["...,1:"].ToArray());
        }
        /********************************************************************
         * Kim smoother using pure Python
         * Parameters
         * ----------
         * regime_transition : array
         * Matrix of regime transition probabilities, shaped either
         * (k_regimes, k_regimes, 1) or if there are time-varying transition
         * probabilities (k_regimes, k_regimes, nobs).
         * filtered_marginal_probabilities : array
         * Array containing Pr[S_t=s_t | Y_t] - the probability of being in each
         * regime conditional on time t information. Shaped (k_regimes, nobs).
         * predicted_joint_probabilities : array
         * Array containing Pr[S_t=s_t, ..., S_{t-order}=s_{t-order} | Y_{t-1}] -
         * the joint probability of the current and previous `order` periods
         * being in each combination of regimes conditional on time t-1
         * information. Shaped (k_regimes,) * (order + 1) + (nobs,).
         * filtered_joint_probabilities : array
         * Array containing Pr[S_t=s_t, ..., S_{t-order}=s_{t-order} | Y_{t}] -
         * the joint probability of the current and previous `order` periods
         * being in each combination of regimes conditional on time t
         * information. Shaped (k_regimes,) * (order + 1) + (nobs,).
         * * *********************************************************************/
        public Tuple<Array, double[,]> py_kim_smoother(
            double[, ,] regime_transition,
            double[,] filtered_marginal_probabilities,
            Array predicted_joint_probabilities,
            Array filtered_joint_probabilities)
        {
            var np = new numpy<double>();//for creating numpy object
            var regime_transition_np = new numpy<double>(regime_transition);
            var filtered_marginal_probabilities_np = new numpy<double>(filtered_marginal_probabilities);
            var predicted_joint_probabilities_np = new numpy<double>(predicted_joint_probabilities);
            var filtered_joint_probabilities_np = new numpy<double>(filtered_joint_probabilities);

            // Dimensions
            int k_regimes = filtered_marginal_probabilities_np.Shape[0];
            int nobs = filtered_joint_probabilities_np.Shape[filtered_joint_probabilities_np.Shape.Length - 1];
            int order = filtered_joint_probabilities_np.dim - 2;

            //initialize the shape of the probabilities array
            var _shape = new int[order + 2];
            _shape[order + 1] = nobs;
            for (var i = 0; i < order + 1; i++)
                _shape[i] = k_regimes;

            // Storage
            var smoothed_joint_probabilities_np = np.zeros(_shape);
            var smoothed_marginal_probabilities_np = np.zeros(k_regimes, nobs);

            // S_T, S_{T-1}, ..., S_{T-r} | T
            smoothed_joint_probabilities_np["..., -1"] = filtered_joint_probabilities_np["..., -1"];

            // Reshape transition so we can use broadcasting
            var shape = new List<int>();
            shape.Add(k_regimes); shape.Add(k_regimes);
            var arr_tmp = new int[order - 1];
            for (int i = 0; i < order - 1; i++)
                arr_tmp[i] = 1;
            shape.AddRange(arr_tmp);
            shape.Add(regime_transition_np.Shape[regime_transition_np.Shape.Length - 1]);
            regime_transition_np = regime_transition_np.reshape(shape.ToArray());

            // Get appropriate subset of transition matrix
            if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1)
                regime_transition_np = regime_transition_np["...," + order.ToString() + ":"];

            // Kim smoother iterations
            var transition_t = 0;
            for (var t = nobs - 2; t >= 0; t--)
            {
                if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1)
                    transition_t = t + 1;
                // S_{t+1}, S_t, ..., S_{t-r+1} | t
                // x = predicted_joint_probabilities[..., t]
                var x = filtered_joint_probabilities_np["...," + t.ToString()] *
                    regime_transition_np["...," + transition_t.ToString()];
                // S_{t+1}, S_t, ..., S_{t-r+2} | T / S_{t+1}, S_t, ..., S_{t-r+2} | t
                var y = smoothed_joint_probabilities_np["...," + (t + 1).ToString()] /
                     predicted_joint_probabilities_np["...," + (t + 1).ToString()];
                // S_{t+1}, S_t, ..., S_{t-r+1} | T
                var tmp_shape = new int[y.Shape.Length + 1];
                tmp_shape[y.Shape.Length] = 1;
                for (int i = 0; i < y.Shape.Length; i++)
                    tmp_shape[i] = y.Shape[i];
                smoothed_joint_probabilities_np["...," + t.ToString()] =
                    x * y.reshape(tmp_shape).sum(0);
            }
            // Get smoothed marginal probabilities S_t | T by integrating out
            // S_{t-k+1}, S_{t-k+2}, ..., S_{t-1}
            smoothed_marginal_probabilities_np = smoothed_joint_probabilities_np;
            for (int i = 1; i < smoothed_marginal_probabilities_np.dim - 1; i++)
                smoothed_marginal_probabilities_np =
                    smoothed_marginal_probabilities_np.sum(-2);

            return Tuple.Create<Array, double[,]>(
                smoothed_joint_probabilities_np.ToArray(),
                (double[,])smoothed_marginal_probabilities_np.ToArray());
        }

    }

    public class MarkovSwitchingParams
    {
        /********************************************************************
        Class to hold parameters in Markov switching models

        Parameters
        ----------
        k_regimes : int
            The number of regimes between which parameters may switch.

        Notes
        -----

        The purpose is to allow selecting parameter indexes / slices based on
        parameter type, regime number, or both.

        Parameters are lexicographically ordered in the following way:

        1. Named type string (e.g. "autoregressive")
        2. Number (e.g. the first autoregressive parameter, then the second)
        3. Regime (if applicable)

        Parameter blocks are set using dictionary setter notation where the key
        is the named type string and the value is a list of boolean values
        indicating whether a given parameter is switching or not.

        For example, consider the following code:

            parameters = MarkovSwitchingParams(k_regimes=2)
            parameters['regime_transition'] = [1,1]
            parameters['exog'] = [0, 1]

        This implies the model has 7 parameters: 4 "regime_transition"-related
        parameters (2 parameters that each switch according to regimes) and 3
        "exog"-related parameters (1 parameter that does not switch, and one 1 that
        does).

        The order of parameters is then:

        1. The first "regime_transition" parameter, regime 0
        2. The first "regime_transition" parameter, regime 1
        3. The second "regime_transition" parameter, regime 0
        4. The second "regime_transition" parameter, regime 1
        5. The first "exog" parameter
        6. The second "exog" parameter, regime 0
        7. The second "exog" parameter, regime 1

        Retrieving indexes / slices is done through dictionary getter notation.
        There are three options for the dictionary key:

        - Regime number (zero-indexed)
        - Named type string (e.g. "autoregressive")
        - Regime number and named type string

        In the above example, consider the following getters:

        >>> parameters[0]
        array([0, 2, 4, 5])
        >>> parameters[1]
        array([1, 3, 4, 6])
        >>> parameters['exog']
        slice(4, 7, None)
        >>> parameters[0, 'exog']
        [4, 5]
        >>> parameters[1, 'exog']
        [4, 7]

        Notice that in the last two examples, both lists of indexes include 4.
        That's because that is the index of the the non-switching first "exog"
        parameter, which should be selected regardless of the regime.

        In addition to the getter, the `k_parameters` attribute is an OrderedDict
        with the named type strings as the keys. It can be used to get the total
        number of parameters of each type:

        >>> parameters.k_parameters['regime_transition']
        4
        >>> parameters.k_parameters['exog']
        3
         *********************************************************************/

        // the number of regimes
        public int k_regimes;

        // the number of parameters for each part
        public int k_params;

        // a dictionary of numbers of parameters for each part
        public Dictionary<string, int> k_parameters;

        // bit array specifying whether or not a parameter is switching for each part
        public Dictionary<string, List<bool>> swicthing;

        ///slices of parameters for each part, e.g.
        ///params[this["autoregressive"]]
        ///should give us autoregressive parameters
        public Dictionary<string, Tuple<int, int>> slices_purpose;

        ///first it is an array, with indices corresponding different regimes
        ///then for each element, it is a dictionary
        public Dictionary<string, List<int>>[] relative_index_regime_purpose;

        ///used by getter with (string,int) or (int,string) signature
        ///where int key represents regime
        ///and string key represents parameter type (e.g. "autoregressive")
        ///together they retrieve indices of parameters corresponding to this regime and type
        public Dictionary<string, List<int>>[] index_regime_purpose;

        ///used by getter with (int) signature
        ///where int key represents regime
        ///it retrieves indices of parameters corresponding to this regime
        public List<int>[] index_regime;

        public MarkovSwitchingParams(int k_regimes)
        {
            // initialization
            this.k_regimes = k_regimes;
            this.k_params = 0;
            this.k_parameters = new Dictionary<string, int>();
            this.swicthing = new Dictionary<string, List<bool>>();
            this.slices_purpose = new Dictionary<string, Tuple<int, int>>();
            this.relative_index_regime_purpose = new Dictionary<string, List<int>>[k_regimes];
            this.index_regime_purpose = new Dictionary<string, List<int>>[k_regimes];
            this.index_regime = new List<int>[k_regimes];
        }

        public object this[string key]
        {
            get { return slices_purpose[key]; }
            set
            {
                // value should be List<bool>
                // specifying whether or not each parameter is switching
                // and the length of value is number of parameters related with key for each regime
                // but here in order to sum over them we use int[]
                List<bool> _value = new List<bool>();
                var temp_k_params = this.k_params;
                k_parameters[key] = _value.Count + _value.Cast<int>().Sum() * (k_regimes - 1);
                k_params += (int)k_parameters[key];
                swicthing[key] = _value;
                slices_purpose[key] = Tuple.Create(temp_k_params, this.k_params);

                foreach (var j in Enumerable.Range(0, k_regimes))
                {
                    this.relative_index_regime_purpose[j][key] = new List<int>();
                    this.index_regime_purpose[j][key] = new List<int>();
                }

                int offset = 0;

                foreach (var i in Enumerable.Range(0, _value.Count))
                {
                    //whether or not the i-th parameter is switching
                    var temp_swicthing = _value[i];
                    foreach (var j in Enumerable.Range(0, this.k_regimes))
                    {
                        //Non-switching parameters
                        if (!temp_swicthing)
                            // then there is only one parameter i
                            this.relative_index_regime_purpose[j][key].Add(offset);
                        //Switching parameters
                        else
                            // then there are k_regimes parameters of i
                            // and this is the j-th one
                            this.relative_index_regime_purpose[j][key].Add(offset + j);
                    }
                    // if this is a switching parameter, switch to k_regimes position
                    // otherwise, switch to 1
                    offset += (!temp_swicthing) ? 1 : k_regimes;

                    //for each regime j 
                    foreach (var j in Enumerable.Range(0, this.k_regimes))
                    {
                        offset = 0;
                        List<int> indices = new List<int>();
                        foreach (var k in relative_index_regime_purpose[j].Keys)
                        {
                            var v = relative_index_regime_purpose[j][k];
                            v = (List<int>)v.Select(x => x + offset);
                            this.index_regime_purpose[j][k] = v;
                            indices.AddRange(v);
                            offset += this.k_parameters[k];
                        }
                        this.index_regime[j] = indices;
                    }
                }
            }
        }
        public List<int> this[int key]
        {
            get { return index_regime[key]; }
        }
        public List<int> this[int key_1, string key_2]
        {
            get { return index_regime_purpose[key_1][key_2]; }
        }
        public List<int> this[string key_1, int key_2]
        {
            get { return index_regime_purpose[key_2][key_1]; }
        }


    }

    public class MarkovSwitching : TimeSeriesModel
    {
        /********************************************************************
        First-order k-regime Markov switching model

        Parameters
        ----------
        endog : array_like
            The endogenous variable.
        k_regimes : integer
            The number of regimes.
        order : integer, optional
            The order of the model describes the dependence of the likelihood on
            previous regimes. This depends on the model in question and should be
            set appropriately by subclasses.
        exog_tvtp : array_like, optional
            Array of exogenous or lagged variables to use in calculating
            time-varying transition probabilities (TVTP). TVTP is only used if this
            variable is provided. If an intercept is desired, a column of ones must
            be explicitly included in this array.

        Notes
        -----
        This model is new and API stability is not guaranteed, although changes
        will be made in a backwards compatible way if possible.

        References
        ----------
        Kim, Chang-Jin, and Charles R. Nelson. 1999.
        "State-Space Models with Regime Switching:
        Classical and Gibbs-Sampling Approaches with Applications".
        MIT Press Books. The MIT Press.
         ********************************************************************/

        //the number of regimes
        public int k_regimes;
        //whether calculate time-varying transition probabilities(TVTP)
        public bool tvtp;
        //The order of the model may be overridden in subclasses
        public int order;
        //
        public int k_tvtp;
        //
        public double[,] exog_tvtp;
        //
        public int nobs;
        public MarkovSwitchingParams parameters;
        public string _initialization;
        private double[] _initial_probabilities;
        public MarkovSwitching_Prepare _prepare;

        public MarkovSwitching(double[] endog, int k_regimes, int order = 0, Array exog_tvtp = null,
            double[] exog = null, object dates = null, string freq = null, string missing = "none") :
            base(endog, exog,
                    new Dictionary<string, object>
                 {
                     {"dates",dates},{"freq",freq},{"missing",missing}
                 })// Initialize the base model
        {
            // Properties
            this.k_regimes = k_regimes;
            this.tvtp = (exog_tvtp != null);
            // The order of the model may be overridden in subclasses
            this.order = order;

            // Exogenous data after preparing
            // TODO add checks for exog_tvtp consistent shape and indices
            this.k_tvtp = _prepare._prepare_exog(exog_tvtp).Item1;
            this.exog_tvtp = _prepare._prepare_exog(exog_tvtp).Item2;

            // Dimensions
            this.nobs = endog.Length;

            // Sanity checks
            if (endog.Rank > 1)
                throw new ArgumentException("Must have univariate endogenous data.");
            if (this.k_regimes < 2)
                throw new ArgumentException("Markov switching models must have at"
                                            + "least two regimes.");
            if (!(this.exog_tvtp == null || this.exog_tvtp.Length == nobs))
                throw new ArgumentException("Time-varying transition probabilities" +
                                            "exogenous array must have the same number" +
                                            "of observations as the endogenous array.");
            // Parameters
            this.parameters = new MarkovSwitchingParams(this.k_regimes);
            var k_transition = this.k_regimes - 1;
            if (tvtp)
                k_transition *= k_tvtp;

            List<bool> temp_list = new List<bool>();
            for (int i = 0; i < k_transition; i++)
                temp_list.Add(true);
            parameters["regime_transition"] = temp_list;

            //Internal model properties: default is steady-state initialization
            this._initialization = "steady-state";
            this._initial_probabilities = null;

        }

        // (int) Number of parameters in the model
        public int k_params { get { return this.parameters.k_params; } }

        public void initialize_steady_state()
        {
            /// Set initialization of regime probabilities to be steady-state values
            /// Notes
            /// -----
            /// Only valid if there are not time-varying transition probabilities.
            if (tvtp)
                throw new ArgumentException("Cannot use steady-state initialization"
                                + "when the regime transition matrix is time-varying.");
            _initialization = "steady-state";
            _initial_probabilities = null;
        }

        public void initialize_known(double[] probabilities, double tol = 1e-8)
        {
            // Set initialization of regime probabilities to use known values
            this._initialization = "known";
            //Sanity checks
            if (probabilities.Length != k_regimes)
                throw new ArgumentException("Initial probabilities must be a vector" +
                                            "of shape (k_regimes,).");
            if (Math.Abs(probabilities.Sum() - 1) >= tol)
                throw new ArgumentException("Initial probabilities vector must sum to one.");
            this._initial_probabilities = probabilities;
        }

        //....
        public double[] initial_probabilities(double[] _params, double[, ,] regime_transition = null)
        {
            //Retrieve initial probabilities

            var np = new numpy<double>();
            var probabilities = (double[])null;
            var regime_transition_np = new numpy<double>(regime_transition);
            if (_initialization == "steady-state")
            {
                if (regime_transition == null)
                {
                    regime_transition = regime_transition_matrix(_params);
                    regime_transition_np = new numpy<double>(regime_transition);
                }
                if (regime_transition_np.dim == 3)
                    regime_transition_np = regime_transition_np["...,0"];//degrade to shape(,,1)
                var m = regime_transition_np.Shape[0];
                var A = np.concatenate((np.eye(m) - regime_transition_np).t(), np.ones(m), 1).t();

                //-----TBC----

                //try:
                //    probabilities = np.linalg.pinv(A)[:, -1]
                //except np.linalg.LinAlgError:
                //    raise RuntimeError('Steady-state probabilities could not be'
                //                       ' constructed.')
            }
            else if (_initialization == "known")
                probabilities = _initial_probabilities;
            else
                throw new TimeoutException("Invalid initialization method selected.");
            return probabilities;
        }

        public double[, ,] _regime_transition_matrix_tvtp(double[] _params, Array exog_tvtp = null)
        {
            double[,] temp_exog_tvtp = null;
            if (exog_tvtp == null)
                temp_exog_tvtp = this.exog_tvtp;
            else// make sure we have 2-dimensional array
                temp_exog_tvtp = (double[,])exog_tvtp;
            nobs = temp_exog_tvtp.Length;
            var regime_transition_matrix_np = new numpy<double>().zeros(k_regimes, k_regimes, nobs);
            var np = new numpy<double>();// for convinience invoke

            // Compute the predicted values from the regression
            for (int i = 0; i < k_regimes; i++)
            {
                // add the parameters to the coefficients list
                var coeffs = parameters[i, "regime_transition"].ToArray();
                var coeffs_np = new numpy<int>(coeffs).reshape(k_regimes - 1, k_tvtp).t();
                var arr1 = (double[,])temp_exog_tvtp;//M * K shape
                var arr2 = (double[,])coeffs_np.ToArray();//K * N shape
                var _m = arr1.GetLength(0);//M
                var _k = 0;//K
                if (arr1.GetLength(1) != arr2.GetLength(0))//Sanity check
                    throw new RankException("the coeffs matrix should match the reshaped one");
                else
                    _k = arr2.GetLength(0);//=arr1.GetLength(1)
                var _n = arr2.GetLength(1);//N
                var arr3 = new double[_m, _n];//the result array
                alglib.rmatrixgemm(_m, _n, _k, 1.0, arr1, 0, 0, 0, arr2, 0, 0, 0, 0, ref arr3, 0, 0);
                string str = ":-1," + i.ToString() + ",:";//":-1,i,:"
                regime_transition_matrix_np[str] = new numpy<double>(arr3).t();
            }

            //Perform the logistic transformation
            var tmp_np = np.concatenate(np.zeros(nobs, k_regimes, 1),
                regime_transition_matrix_np[":-1,:,:"].t()).t();

            var tmp2_np = regime_transition_matrix_np[":-1,:,:"] - tmp_np.exp().sum(0).log();
            regime_transition_matrix_np[":-1,:,:"] = tmp2_np.exp();

            // Compute the last column of the transition matrix
            regime_transition_matrix_np["-1,:,0"] =
                1.0 - regime_transition_matrix_np[":-1,:,0"].sum(0);

            return (double[, ,])regime_transition_matrix_np.ToArray();
        }

        public double[, ,] regime_transition_matrix(double[] _params, Array exog_tvtp = null)
        {
            /********************************************************************
             * 
             * Construct the left-stochastic transition matrix
             * Notes
             * -----
             * This matrix will either be shaped (k_regimes, k_regimes, 1) or if there
             * are time-varying transition probabilities, it will be shaped
             * (k_regimes, k_regimes, nobs).
             * 
             * The (i,j)th element of this matrix is the probability of transitioning
             * from regime j to regime i; thus the previous regime is represented in a
             * column and the next regime is represented by a row.
             * 
             * It is left-stochastic, meaning that each column sums to one (because
             * it is certain that from one regime (j) you will transition to *some
             * other regime*).
             *********************************************************************/
            var _params_np = new numpy<double>(_params);
            double[, ,] regime_transition_matrix;
            if (!tvtp)
            {
                var tmp = this.parameters["regime_transition"] as Tuple<int, int>;
                var start = tmp.Item1;
                var end = tmp.Item2;
                int[,] s_e = new int[,] { { start, end } };
                var regime_transition_matrix_np = new numpy<double>().zeros(k_regimes, k_regimes, 1);
                regime_transition_matrix_np[":-1,:,0"] =
                    new numpy<double>(_params_np[s_e]).reshape(k_regimes - 1, k_regimes);// _params_np's ndim=1
                //regime_transition_matrix[-1, :, 0] =(1 - np.sum(regime_transition_matrix[:-1, :, 0], axis=0))             
                regime_transition_matrix_np["-1, :, 0"] = 1 - regime_transition_matrix_np[":-1, :, 0"].sum(0);
                regime_transition_matrix = (double[, ,])regime_transition_matrix_np.ToArray();
            }
            else
                regime_transition_matrix = this._regime_transition_matrix_tvtp(_params, exog_tvtp);

            return regime_transition_matrix;
        }

        //public predict(double[]_params, int start=null, int end=null, double[] probabilities=null,bool conditional = false)
        //public predict(double[]_params, int start=null, int end=null, string probabilities=null,bool conditional = false)

        public Array predict_conditional(double[] _params)
        {
            //In-sample prediction, conditional on the current, and possibly past,
            //regimes

            //Parameters
            //----------
            //params : array_like
            //Array of parameters at which to perform prediction.

            //Returns
            //-------
            //predict : array_like
            //Array of predictions conditional on current, and possibly past,
            //regimes
            throw new NotImplementedException();
        }

        public Array _conditional_likelihoods(double[] _params)
        {
            //Compute likelihoods conditional on the current period's regime (and
            //the last self.order periods' regimes if self.order > 0).
            //Must be implemented in subclasses.
            throw new NotImplementedException();
        }

        public Tuple<double[, ,], double[], Array, Tuple<double[,], Array, double[], Array>> _filter
            (double[] _params, double[, ,] regime_transition = null)
        {
            // Get the regime transition matrix if not provided
            if (regime_transition == null)
                regime_transition = regime_transition_matrix(_params);
            // Get the initial probabilities
            _initial_probabilities = initial_probabilities(_params, regime_transition);
            // Compute the conditional likelihoods
            var conditional_likelihoods = _conditional_likelihoods(_params);
            // Apply the filter
            var r = Tuple.Create<double[, ,], double[], Array, Tuple<double[,], Array, double[], Array>>
                (regime_transition, _initial_probabilities, conditional_likelihoods,
                _prepare.cy_hamilton_filter(_initial_probabilities, regime_transition, conditional_likelihoods));
            return r;
        }
        //....
        public HamiltonFilterResults filter(double[] _params, bool transformed = true, string cov_type = null, Dictionary<string, string> cov_kwds = null,
            bool return_raw = false/*,MarkovSwitchingResults result_class = null, MarkovSwitchingResults result_wrapper_class = null*/)
        {
            /* Apply the Hamilton filter

            Parameters
            ----------
            params : array_like
                Array of parameters at which to perform filtering.
            transformed : boolean, optional
                Whether or not `params` is already transformed. Default is True.
            cov_type : str, optional
                See `fit` for a description of covariance matrix types
                for results object.
            cov_kwds : dict or None, optional
                See `fit` for a description of required keywords for alternative
                covariance estimators
            return_raw : boolean,optional
                Whether or not to return only the raw Hamilton filter output or a
                full results object. Default is to return a full results object.
            results_class : type, optional
                A results class to instantiate rather than
                `MarkovSwitchingResults`. Usually only used internally by
                subclasses.
            results_wrapper_class : type, optional
                A results wrapper class to instantiate rather than
                `MarkovSwitchingResults`. Usually only used internally by
                subclasses.

            Returns
            -------
            MarkovSwitchingResults
            ***********************************************************************/
            var _params_np = new numpy<double>(_params);
            if (!transformed)
                _params_np = transform_params(_params_np);
            // Save the parameter names..
            //self.data.param_names = self.param_names

            // Get the result
            var tmp_D = new Dictionary<string, Array>();
            var tmp_t = _filter((double[])_params_np.ToArray());
            tmp_D.Add("regime_transition", tmp_t.Item1);
            tmp_D.Add("initial_probabilities", tmp_t.Item2);
            tmp_D.Add("conditional_likelihoods", tmp_t.Item3);
            tmp_D.Add("filtered_marginal_probabilities", tmp_t.Item4.Item1);
            tmp_D.Add("predicted_joint_probabilities", tmp_t.Item4.Item2);
            tmp_D.Add("joint_likelihoods", tmp_t.Item4.Item3);
            tmp_D.Add("filtered_joint_probabilities", tmp_t.Item4.Item4);
            var result = new HamiltonFilterResults(this, tmp_D);

            // Wrap in a results object..

            return result;
        }

        public Tuple<Array, double[,]> _smooth(double[] _params, double[,] filtered_marginal_probabilities, Array predicted_joint_probabilities,
            Array filtered_joint_probabilities, double[, ,] regime_transition = null)
        {
            MarkovSwitching_Prepare tmp = new MarkovSwitching_Prepare();
            // Get the regime transition matrix
            if (regime_transition == null)
            {
                var regime_transition_np = new numpy<double>(regime_transition_matrix(_params));
                regime_transition = (double[, ,])regime_transition_np.ToArray();
            }
            return tmp.py_kim_smoother(regime_transition,
                                       filtered_marginal_probabilities,
                                       predicted_joint_probabilities,
                                       filtered_joint_probabilities);
        }
        //....
        public KimSmootherResults smooth(double[] _params, bool transformed = true, string cov_type = null, Dictionary<string, string> cov_kwds = null,
            bool return_raw = false/*, MarkovSwitchingResults result_class = null, MarkovSwitchingResults result_wrapper_class = null*/)
        {
            /* Apply the Kim smoother and Hamilton filter

             Parameters
             ----------
             params : array_like
                 Array of parameters at which to perform filtering.
             transformed : boolean, optional
                 Whether or not `params` is already transformed. Default is True.
             cov_type : str, optional
                 See `fit` for a description of covariance matrix types
                 for results object.
             cov_kwds : dict or None, optional
                 See `fit` for a description of required keywords for alternative
                 covariance estimators
             return_raw : boolean,optional
                 Whether or not to return only the raw Hamilton filter output or a
                 full results object. Default is to return a full results object.
             results_class : type, optional
                 A results class to instantiate rather than
                 `MarkovSwitchingResults`. Usually only used internally by
                 subclasses.
             results_wrapper_class : type, optional
                 A results wrapper class to instantiate rather than
                 `MarkovSwitchingResults`. Usually only used internally by
                 subclasses.

             Returns
             -------
             MarkovSwitchingResults
             **************************************************************/
            var _params_np = new numpy<double>(_params);
            if (!transformed)
                _params_np = transform_params(_params_np);
            // Save the parameter names..
            //self.data.param_names = self.param_names

            // Hamilton filter
            var tmp_D = new Dictionary<string, Array>();
            var tmp_t = _filter((double[])_params_np.ToArray());
            tmp_D.Add("regime_transition", tmp_t.Item1);
            tmp_D.Add("initial_probabilities", tmp_t.Item2);
            tmp_D.Add("conditional_likelihoods", tmp_t.Item3);
            tmp_D.Add("filtered_marginal_probabilities", tmp_t.Item4.Item1);
            tmp_D.Add("predicted_joint_probabilities", tmp_t.Item4.Item2);
            tmp_D.Add("joint_likelihoods", tmp_t.Item4.Item3);
            tmp_D.Add("filtered_joint_probabilities", tmp_t.Item4.Item4);

            // Kim smoother
            var _out = _smooth((double[])_params_np.ToArray(), (double[,])tmp_D["filtered_marginal_probabilities"],
                tmp_D["predicted_joint_probabilities"], tmp_D["filtered_joint_probabilities"]);
            tmp_D.Add("smoothed_joint_probabilities", _out.Item1);
            tmp_D.Add("smoothed_marginal_probabilities", _out.Item2);
            var result = new KimSmootherResults(this, tmp_D);

            // Wrap in a results object..

            return result;
        }

        public numpy<double> loglikeobs(double[] _params, bool transformed = true)
        {
            //Loglikelihood evaluation for each period
            //Parameters
            //----------
            //params : array_like
            //    Array of parameters at which to evaluate the loglikelihood
            //    function.
            //transformed : boolean, optional
            //    Whether or not `params` is already transformed. Default is True.

            var _params_np = new numpy<double>(_params);
            if (!transformed)
                _params_np = new numpy<double>(transform_params(_params_np));
            var result = _filter((double[])_params_np.ToArray());
            var _result = new numpy<double>(result.Item4.Item3);//obtain the joint-likelihood prob.

            return _result.log();
        }
        public numpy<double> loglikeobs(numpy<double> _params, bool transformed = true)
        {
            //Loglikelihood evaluation for each period
            //Parameters
            //----------
            //params : array_like
            //    Array of parameters at which to evaluate the loglikelihood
            //    function.
            //transformed : boolean, optional
            //    Whether or not `params` is already transformed. Default is True.

            var _params_np = new numpy<double>(_params);
            if (!transformed)
                _params_np = new numpy<double>(transform_params(_params_np));
            var result = _filter((double[])_params_np.ToArray());
            var _result = new numpy<double>(result.Item4.Item3);//obtain the joint-likelihood prob.

            return _result.log();
        }

        public double loglike(double[] _params, bool transformed = true)
        {
            //Loglikelihood evaluation
            //Parameters
            //----------
            //params : array_like
            //    Array of parameters at which to evaluate the loglikelihood
            //    function.
            //transformed : boolean, optional
            //    Whether or not `params` is already transformed. Default is True.

            return loglikeobs(_params, transformed).sum();
        }
        public double loglike(numpy<double> _params, bool transformed = true)
        {
            //Loglikelihood evaluation
            //Parameters
            //----------
            //params : array_like
            //    Array of parameters at which to evaluate the loglikelihood
            //    function.
            //transformed : boolean, optional
            //    Whether or not `params` is already transformed. Default is True.

            return loglikeobs(_params, transformed).sum();
        }

        //public score(double[] _params, bool transformed = true)
        //public score_obs(double[] _params, bool transformed = true)
        //public hessian(double[] _params, bool transformed = true)
        //....
        public numpy<double> fit(double[] start_params = null, bool transformed = true, string cov_type = "approx")
        {
            var start_params_np = new numpy<double>();
            if (start_params == null)
            {
                start_params_np = new numpy<double>(this.start_params);
                transformed = true;
            }
            else
                start_params_np = new numpy<double>(start_params);

            return null;
        }
        //....
        public KimSmootherResults fit(double[] start_params = null, bool transformed = true, string cov_type = "approx", double tmp = 1)
        {
            var start_params_np = new numpy<double>();
            if (start_params == null)
            {
                start_params_np = new numpy<double>(this.start_params);
                transformed = true;
            }
            else
                start_params_np = new numpy<double>(start_params);

            return null;
        }

        public numpy<double> _fit_em(double[] start_params = null, bool transformed = true, string cov_type = "none", int maxiter = 50, double tolerance = 1e-6)
        {
            var start_params_np = new numpy<double>();
            if (start_params == null)
            {
                start_params_np = new numpy<double>(this.start_params);
                transformed = true;
            }
            else
                start_params_np = new numpy<double>(start_params);
            if (!transformed)
                start_params_np = transform_params(start_params_np);

            // Perform expectation-maximization
            var llf = new List<double>();// this is a list stores the llf value from HamiltonFilterResult class
            var _params = new List<numpy<double>>();
            _params.Add(start_params_np);
            var i = 0;
            double delta = 0;
            while (i < maxiter && (i < 2 || delta > tolerance))
            {
                var _out = _em_iteration(_params[_params.Count - 1]);
                llf.Add(_out.Item1.llf);
                _params.Add(_out.Item2);
                if (i > 0)//not checked....
                    delta = 2 * (llf[llf.Count - 1] - llf[llf.Count - 2]) / Math.Abs(llf[llf.Count - 1] + llf[llf.Count - 2]);
                i += 1;
            }

            // Just return the fitted parameters if requested
            return _params[_params.Count - 1];
        }
        //....
        public HamiltonFilterResults _fit_em(double[] start_params = null, bool transformed = true, string cov_type = "none",
            Dictionary<string, string> cov_kwds = null, bool full_output = true, int maxiter = 50, double tolerance = 1e-6)
        {
            var start_params_np = new numpy<double>();
            if (start_params == null)
            {
                start_params_np = new numpy<double>(this.start_params);
                transformed = true;
            }
            else
                start_params_np = new numpy<double>(start_params);
            if (!transformed)
                start_params_np = transform_params(start_params_np);

            // Perform expectation-maximization
            var llf = new List<double>();// this is a list stores the llf value from HamiltonFilterResult class
            var _params = new List<numpy<double>>();
            _params.Add(start_params_np);
            var i = 0;
            double delta = 0;
            while (i < maxiter && (i < 2 || delta > tolerance))
            {
                var _out = _em_iteration(_params[_params.Count - 1]);
                llf.Add(_out.Item1.llf);
                _params.Add(_out.Item2);
                if (i > 0)//not checked....
                    delta = 2 * (llf[llf.Count - 1] - llf[llf.Count - 2]) / Math.Abs(llf[llf.Count - 1] + llf[llf.Count - 2]);
                i += 1;
            }
            // Construct the results class if desired
            var result = filter((double[])_params[_params.Count - 1].ToArray(), true, cov_type, cov_kwds);
            // Save the output
            var em_retvals = new Dictionary<string, object>();
            var em_settings = new Dictionary<string, double>();
            if (full_output)
            {
                em_retvals = new Dictionary<string, object>{
                             {"params",new numpy<double>(_params.ToArray())},
                             {"llf",new numpy<double>(llf.ToArray())},
                             {"iter",i}};
                em_settings = new Dictionary<string, double>{
                              {"tolerance",tolerance},{"maxiter",maxiter}};
            }
            else
            {
                em_retvals = null;
                em_settings = null;
            }
            //....result.mle_retvals = em_retvals
            //....result.mle_settings = em_settings
            return result;
        }

        public Tuple<KimSmootherResults, numpy<double>> _em_iteration(numpy<double> _params_np_0)
        {
            /* EM iteration
             * Notes
             * -----
             * The EM iteration in this base class only performs the EM step for
             * non-TVTP transition probabilities.
             * ******************************************************************/
            var _params_np_1 = new numpy<double>().zeros(_params_np_0.Shape);
            // Smooth at the given parameters
            var result = smooth((double[])_params_np_0.ToArray(), true, null, null, true);

            // The EM with TVTP is not yet supported, just return the previous
            // iteration parameters            
            if (tvtp)
            {
                var tmp = this.parameters["regime_transition"] as Tuple<int, int>;
                int[,] s_e = new int[,] { { tmp.Item1, tmp.Item2 } };// slice array
                _params_np_1[s_e] = _params_np_0[s_e];
            }
            else
            {
                var regime_transition_np = _em_regime_transition(result);
                for (var i = 0; i < k_regimes; i++)
                {
                    int[] L = parameters[i, "regime_transition"].ToArray();// must be a 1-d array
                    for (int j = 0; i < L.Length; i++)// fancy index, only use for 1-d array !
                        _params_np_1[L[j]] = regime_transition_np[i];// both should be 1-d array !
                }
            }
            return Tuple.Create<KimSmootherResults, numpy<double>>(result, _params_np_1);
        }

        public numpy<double> _em_regime_transition(KimSmootherResults result)
        {
            // EM step for regime transition probabilities

            // Marginalize the smoothed joint probabilites to just S_t, S_{t-1} | T
            var tmp = result.smoothed_joint_probabilities_np;
            for (var i = 0; i < tmp.dim - 3; i++)
                tmp = tmp.sum(-2);
            var smoothed_joint_probabilities_np = tmp;
            var k_transition = parameters[0, "regime_transition"].Count;
            var regime_transition_np = new numpy<double>().zeros(k_regimes, k_transition);
            for (var i = 0; i < k_regimes; i++)// S_{t_1}
            {
                for (var j = 0; j < k_regimes - 1; j++)// S_t
                    regime_transition_np[i, j] = smoothed_joint_probabilities_np[j.ToString() + ":" + i.ToString()].sum() /
                                                 result.smoothed_marginal_probabilities_np[i].sum();
                var delta = regime_transition_np[i].sum() - 1;
                if (delta > 0)
                {
                    Console.WriteLine("Invalid regime transition probabilities" +
                                      " estimated in EM iteration; probabilities have" +
                                      " been re-scaled to continue estimation.");
                    regime_transition_np[i] = regime_transition_np[i] / (1 + delta + 1e-6);
                }
            }
            return regime_transition_np;
        }

        public numpy<double> _start_params_search(int reps, double[] start_params = null, bool transformed = true,
            int em_iter = 5, double scale = 1.0)
        {
            var start_params_np = new numpy<double>();
            if (start_params == null)
            {
                start_params_np = new numpy<double>(this.start_params);
                transformed = true;
            }
            else
                start_params_np = new numpy<double>(start_params);

            // Random search is over untransformed space
            if (transformed)
                start_params_np = untransform_params(start_params_np);

            // Construct the standard deviations
            var scale_np = new numpy<double>().ones(k_params) * scale;//1-d

            // Construct the random variates
            var variates_np = new numpy<double>().zeros(reps, k_params);
            Random ran = new Random();
            for (var i = 0; i < k_params; i++)
            {
                var arr = new double[reps];
                for (var j = 0; j < reps; j++)
                    arr[j] = ran.NextDouble() - 0.5;//uniform distribution[-0.5,0.5]
                var np = new numpy<double>(arr);
                variates_np[":," + i.ToString()] = scale_np[i] * np;//scale_np[i] is a scalar numpy obejct
            }
            var llf = loglike(start_params_np, false);
            var _params_np = start_params_np;
            for (var i = 0; i < reps; i++)
            {
                try
                {
                    numpy<double> proposed_params_np = _fit_em((double[])(start_params_np + variates_np[i]).ToArray()
                        , false, null, em_iter);
                    double proposed_llf = loglike(proposed_params_np);
                    if (proposed_llf > llf)
                    {
                        llf = proposed_llf;
                        _params_np = untransform_params(proposed_params_np);
                    }
                }
                catch { continue; }
            }

            // Return transformed parameters
            return transform_params(_params_np);
        }

        public double[] start_params
        {
            // (array) Starting parameters for maximum likelihood estimation.
            get
            {
                var _params_np = new numpy<double>().zeros(k_params);
                if (this.tvtp)
                {
                    var tmp = this.parameters["regime_transition"] as Tuple<int, int>;
                    var arr = new int[tmp.Item2 - tmp.Item1];
                    // _params_np[item1:item2] = 0
                    _params_np[tmp.Item1.ToString() + ":" + tmp.Item2.ToString()] = new numpy<double>(arr);
                }
                else
                {
                    var tmp = this.parameters["regime_transition"] as Tuple<int, int>;
                    var arr = new int[] { tmp.Item2 - tmp.Item1 };//shape
                    var tmp_np = new numpy<double>().ones(arr) / k_regimes;
                    _params_np[tmp.Item1.ToString() + ":" + tmp.Item2.ToString()] = tmp_np;
                }
                return (double[])_params_np.ToArray();
            }
        }
        //....
        public List<string> param_names
        {
            //(list of str) List of human readable parameter names (for parameters actually included in the model).
            get
            {
                var params_names = new numpy<string>().zeros(k_params);//1-d

                // Transition probabilities
                if (tvtp)
                {
                }
                else
                {
                }
                return new List<string>((string[])params_names.ToArray());
            }
        }

        public numpy<double> transform_params(numpy<double> unconstrained_np)
        {
            //Transform unconstrained parameters used by the optimizer to constrained
            //parameters used in likelihood evaluation
            //Parameters
            //----------
            //unconstrained : array_like(1-D)
            //    Array of unconstrained parameters used by the optimizer, to be
            //    transformed.
            //Returns
            //-------
            //constrained : array_like
            //    Array of constrained parameters which may be used in likelihood
            //    evalation.
            //Notes
            //-----
            //In the base class, this only transforms the transition-probability-
            //related parameters.
            var constrained_np = new numpy<double>(unconstrained_np);
            //TBC------
            return constrained_np;
        }

        public numpy<double> _untransform_logistic(numpy<double> unconstrained_np, numpy<double> constarined_np)
        {
            // Function to allow using a numerical root-finder to reverse the logistic transform.
            //
            var resid_np = new numpy<double>().zeros(unconstrained_np.Shape);
            var exp = unconstrained_np.exp();
            var sum_exp = exp.sum();
            for (int i = 0; i < unconstrained_np.Length; i++)
            {
                var _i = new int[] { i };
                resid_np[_i] = unconstrained_np[_i] - Math.Log(1.0 + sum_exp - exp[_i]) + Math.Log(1 / constarined_np[_i] - 1);
            }
            return resid_np;
        }
        //....
        public numpy<double> untransform_params(numpy<double> constrained_np)
        {
            //Transform constrained parameters used in likelihood evaluation
            //to unconstrained parameters used by the optimizer
            //Parameters
            //----------
            //constrained : array_like(1-D)
            //    Array of constrained parameters used in likelihood evalution, to be
            //    transformed.
            //Returns
            //-------
            //unconstrained : array_like
            //    Array of unconstrained parameters used by the optimizer.
            //Notes
            //-----
            //In the base class, this only untransforms the transition-probability-
            //related parameters.
            var unconstrained_np = constrained_np;

            // Nothing to do for transition probabilities if TVTP
            if (this.tvtp)
            {
                var tmp = this.parameters["regime_transition"] as Tuple<int, int>;
                unconstrained_np[tmp.Item1.ToString() + ":" + tmp.Item2.ToString()]
                    = constrained_np[tmp.Item1.ToString() + ":" + tmp.Item2.ToString()];
            }
            // Otherwise reverse logistic transformation
            else
            {
                for (var i = 0; i < k_regimes; i++)
                {
                    int[] s = parameters[i, "regime_transition"].ToArray();
                    if (k_regimes == 2)
                    {
                        for (var j = 0; j < s.Length; j++)//fancy index in numpy?
                            unconstrained_np[j] = -(1.0 / constrained_np[j] - 1).log();
                    }
                    else
                    { }
                }
            }
            return unconstrained_np;
        }

    }

    public class HamiltonFilterResults
    {
        /*Results from applying the Hamilton filter to a state space model.

        Parameters
        ----------
        model : Representation
            A Statespace representation

        Attributes
        ----------
        nobs : int
            Number of observations.
        k_endog : int
            The dimension of the observation series.
        k_regimes : int
            The number of unobserved regimes.
        regime_transition : array
            The regime transition matrix.
        initialization : str
            Initialization method for regime probabilities.
        initial_probabilities : array
            Initial regime probabilities
        conditional_likelihoods : array
            The likelihood values at each time period, conditional on regime.
        predicted_joint_probabilities : array
            Predicted joint probabilities at each time period.
        filtered_marginal_probabilities : array
            Filtered marginal probabilities at each time period.
        filtered_joint_probabilities : array
            Filtered joint probabilities at each time period.
        joint_likelihoods : array
            The likelihood values at each time period.
        llf_obs : array
            The loglikelihood values at each time period.
        llf : double / decimal 
            the value of sum of the llf_obs.
         ********************************************************************/

        public MarkovSwitching _model;
        public int _nobs;
        public int _order;
        public int _k_regimes;
        private string _initialization;

        public numpy<double> regime_transition_np;
        public numpy<double> initial_probabilities_np;
        public numpy<double> conditional_likelihoods_np;
        public numpy<double> filtered_marginal_probabilities_np;
        public numpy<double> predicted_joint_probabilities_np;
        public numpy<double> filtered_joint_probabilities_np;
        public numpy<double> joint_likelihoods_np;
        public numpy<double> llf_obs;
        public double llf;

        public numpy<double> _predicted_marginal_probabilities_np;// variable for temporoary use

        public HamiltonFilterResults(MarkovSwitching model, Dictionary<string, Array> result)
        {
            this._model = model;
            this._nobs = model.nobs;
            this._order = model.order;
            this._k_regimes = model.k_regimes;

            regime_transition_np = new numpy<double>(result["regime_transition"]);
            initial_probabilities_np = new numpy<double>(result["initial_probabilities"]);
            conditional_likelihoods_np = new numpy<double>(result["conditional_likelihoods"]);
            predicted_joint_probabilities_np = new numpy<double>(result["predicted_joint_probabilities"]);
            filtered_marginal_probabilities_np = new numpy<double>(result["filtered_marginal_probabilities"]);
            filtered_joint_probabilities_np = new numpy<double>(result["filtered_joint_probabilities"]);
            joint_likelihoods_np = new numpy<double>(result["joint_likelihoods"]);

            this._initialization = model._initialization;
            this.llf_obs = new numpy<double>(this.joint_likelihoods_np).log();
            this.llf = new numpy<double>(this.llf).sum();

            // Subset transition if necessary (e.g. for Markov autoregression)
            if (regime_transition_np.Shape[regime_transition_np.Shape.Length - 1] > 1 && _order > 0)
                regime_transition_np = regime_transition_np["...," + _order.ToString() + ":"];

            // Cache for predicted marginal probabilities
            _predicted_marginal_probabilities_np = null;
        }

        public numpy<double> predicted_marginal_probabilities_np
        {
            get
            {
                if (_predicted_marginal_probabilities_np == null)
                {
                    _predicted_marginal_probabilities_np = predicted_joint_probabilities_np;
                    for (int i = 0; i < _predicted_marginal_probabilities_np.dim - 2; i++)
                        _predicted_marginal_probabilities_np =
                            _predicted_marginal_probabilities_np.sum(-2);
                }
                return _predicted_marginal_probabilities_np;
            }
        }
        public numpy<double> expected_durations
        {
            // (array) Expected duration of a regime, possibly time-varying.
            get
            {
                return 1.0 / (1.0 - new numpy<double>().diagonal(regime_transition_np).squeeze());
            }
        }
    }

    public class KimSmootherResults : HamiltonFilterResults
    {
        /* Results from applying the Kim smoother to a Markov switching model.

        Parameters
        ----------
        model : MarkovSwitchingModel
            The model object.
        result : dict
            A dictionary containing two keys: 'smoothd_joint_probabilities' and
            'smoothed_marginal_probabilities'.

        Attributes
        ----------
        nobs : int
            Number of observations.
        k_endog : int
            The dimension of the observation series.
        k_states : int
            The dimension of the unobserved state process. 
         ******************************************************************* */

        public numpy<double> smoothed_joint_probabilities_np;
        public numpy<double> smoothed_marginal_probabilities_np;

        public KimSmootherResults(MarkovSwitching model, Dictionary<string, Array> result)
            : base(model, result)
        {
            this.smoothed_joint_probabilities_np = new numpy<double>(result["smoothed_joint_probabilities"]);
            this.smoothed_marginal_probabilities_np = new numpy<double>(result["smoothed_marginal_probabilities"]);
        }
    }
    //....
    public class MarkovSwitchingResults : TimeSeriesModelResults
    {
        public object data;//....
        public object filter_result;
        public KimSmootherResults smoother_results;
        public int nobs;
        public int order;
        public int k_regimes;
        public string cov_type;
        public Dictionary<string, string> cov_kwds;
        public string _cov_approx_complex_step;
        public string _cov_approx_centered;

        public MarkovSwitchingResults(
            MarkovSwitching model, double[] _params,
            object results,
            Dictionary<string, string> cov_kwds = null,
            Dictionary<string, object> kwargs = null,
            string cov_type = "opg")
            : base(model, _params, null)
        {
            //this.data = model.data;....

            // Save the filter / smoother output
            this.filter_result = results;
            if (results.GetType() == typeof(KimSmootherResults))
                this.smoother_results = (KimSmootherResults)results;
            else
                this.smoother_results = null;

            // dimensions
            this.nobs = model.nobs;
            this.order = model.order;
            this.k_regimes = model.k_regimes;

            // Setup covariance matrix notes dictionary
            this.cov_kwds = cov_kwds;
            this.cov_type = cov_type;

            // Set up the cache....

            // Handle covariance matrix calculation



        }
    }

    public class MarkovSwitchingResultsWrapper : ResultsWrapper
    { }

}
