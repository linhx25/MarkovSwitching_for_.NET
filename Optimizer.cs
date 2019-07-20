using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using NumericalMethod;

namespace StatsModels
{
    // function pointers
    public delegate double FuncDelegate(double[] x);
    public delegate double[] GradientDelegate(double[] x);
    public delegate double[,] HessianDelegate(double[] x);


    public class Optimizer
    {
        public delegate
            Tuple<double[], Dictionary<string,object>>
            FitDelegate(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian);

        public static void _check_method(string method, List<string> methods)
        {
            if (!methods.Exists(x => x == method))
            {
                string message = "Unknown fit method " + method;
                throw new ArgumentException(message);
            }
        }


        /// <summary>
        /// Fit function for any model with an objective function.
        /// </summary>
        /// <param name="objective"></param>
        /// <param name="gradient"></param>
        /// <param name="start_params">array-like, optional
        /// Initial guess of the solution for the loglikelihood maximization.
        /// The default is an array of zeros.</param>
        /// <param name="fargs"></param>
        /// <param name="kwargs"></param>
        /// <param name="hessian"></param>
        /// <param name="method"></param>
        /// <param name="maxiter"></param>
        /// <param name="full_output"></param>
        /// <param name="disp"></param>
        /// <param name="callback"></param>
        /// <param name="retall"></param>
        /// <returns></returns>
        public
            Tuple<double[], Dictionary<string,object>, Dictionary<string, object>>
            _fit(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params=null,
            object fargs=null,
            Dictionary<string, object> kwargs=null,
            HessianDelegate hessian = null,
            string method = "cg",
            int maxiter = 100,
            bool full_output = true,
            bool disp = true,
            alglib.ndimensional_rep callback = null,
            bool retall = true)
        {
            // Default value is array of zeros
            if (start_params == null)
            {
                var n=10;
                start_params = new double[n];
            }
            Dictionary<string, FitDelegate>
                fit_funcs = new Dictionary<string, FitDelegate>
                {
                    {"bc",_fit_bc},
                    {"bleic",_fit_bleic},
                    {"cg",_fit_cg},
                    {"comp",_fit_comp},
                    {"lbfgs",_fit_lbfgs},
                    {"lm",_fit_lm},
                    {"nlc",_fit_nlc},
                    {"ns",_fit_ns},
                    {"qp",_fit_qp}
                };
            string[] _methods = { "bc", "bleic", "cg", "comp", "lbfgs", "lm", "nlc", "ns", "qp" };
            List<string> methods = new List<string>(_methods);
            _check_method(method, methods);

            var func = fit_funcs[method];
            var _output = func(objective, gradient, start_params, fargs, kwargs, disp, maxiter, callback, retall, full_output, hessian);
            var xopt = _output.Item1;
            var retvals = _output.Item2;
            Dictionary<string, object>
                optim_settings = new Dictionary<string, object>
                {
                    {"optimizer",method},
                    {"start_params",start_params},
                    {"maxiter",maxiter},
                    {"full_output",full_output},
                    {"disp",disp},
                    {"fargs",fargs},
                    {"callback",callback},
                    {"retall",retall}
                };
            optim_settings = (Dictionary<string, object>)optim_settings.Concat(kwargs);
            return Tuple.Create(xopt, retvals, optim_settings);
        }

        /// <summary>
        /// Wrapping function and gradient delegates into alglib version of gradient delegate
        /// </summary>
        /// <param name="objective"></param>
        /// <param name="gradient"></param>
        /// <returns></returns>
        public static alglib.ndimensional_grad _get_alglib_grad(
            FuncDelegate objective,
            GradientDelegate gradient)
        {
            return (double[] arg, ref double func, double[] grad, object obj) =>
                {
                    func = objective(arg);
                    grad = gradient(arg);
                };
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_bc(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_bleic(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_cg(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            var n = start_params.Length;
            double[] x = new double[n];
            double epsg = 0.0000000001;
            double epsf = 0;
            double epsx = 0;
            alglib.mincgstate state;
            alglib.mincgreport rep;
            alglib.mincgcreate(start_params, out state);
            alglib.mincgsetcond(state, epsg, epsf, epsx, maxiter);
            var _grad = _get_alglib_grad(objective, gradient);
            alglib.mincgoptimize(state, _grad, callback, null);
            alglib.mincgresults(state, out x, out rep);
            // parse cg report into key-value pairs
            Dictionary<string, object> retvals = new Dictionary<string, object>();
            retvals.Add("iterationscount", rep.iterationscount);
            retvals.Add("nfev", rep.nfev);
            retvals.Add("varidx", rep.varidx);
            retvals.Add("terminationtype", rep.terminationtype);
            return Tuple.Create(x, retvals);
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_comp(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_lbfgs(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_lm(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_nlc(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_ns(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }

        public static
            Tuple<double[], Dictionary<string,object>>
            _fit_qp(
            FuncDelegate objective,
            GradientDelegate gradient,
            double[] start_params,
            object fargs,
            Dictionary<string, object> kwargs,
            bool disp,
            int maxiter,
            alglib.ndimensional_rep callback,
            bool retall,
            bool full_output,
            HessianDelegate hessian)
        {
            throw new NotImplementedException();
        }
    }
}
