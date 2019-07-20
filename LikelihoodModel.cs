using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsModels
{
    public class LikelihoodModel:Model
    {
        public LikelihoodModel(double[] endog,double[] exog=null,Dictionary<string,object> kwargs=null):base(endog,exog,kwargs)
        {
            initialize();
        }
        /// <summary>
        /// Initialize (possibly re-initialize) a Model instance. For
        /// instance, the design matrix of a linear model may change
        /// and some things must be recomputed.
        /// </summary>
        public void initialize()
        {
            
        }
        public double loglike()
        {
            throw new NotImplementedException();
        }
        public double[] score()
        {
            throw new NotImplementedException();
        }
        public double[,] information()
        {
            throw new NotImplementedException();
        }
        public double[,] hessian()
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Fit method for likelihood based models
        /// </summary>
        /// <param name="start_params">array-like, optional
        /// Initial guess of the solution for the loglikelihood maximization.
        /// The default is an array of zeros.</param>
        /// <param name="method">str, optional
        /// The `method` determines which solver from `alglib.optimization` package
        /// is used, and it can be chosen from among the following strings:
        /// - 'cg' for conjugate gradient
        /// - 'bc' for Box constrained optimizer with fast activation of multiple constraints per step
        /// - 'comp' for Backward compatibility functions
        /// - 'nlc' for Nonlinearly constrained optimizer
        /// - 'ns' for Nonsmooth constrained optimizer
        /// - 'bleic' for Bound constrained optimizer with additional linear equality/inequality constraints
        /// - 'lbfgs' for limited-memory BFGS with optional box constraints
        /// - 'qp' for Constrained quadratic programming
        /// - 'lm' for 	Improved Levenberg-Marquardt optimizer</param>
        /// <param name="maxiter">int, optional
        /// The maximum number of iterations to perform.</param>
        /// <param name="full_output">bool, optional
        /// Set to True to have all available output in the Results object's
        /// mle_retvals attribute. The output is dependent on the solver.
        /// See LikelihoodModelResults notes section for more information.</param>
        /// <param name="disp">bool, optional
        /// Set to True to print convergence messages.</param>
        /// <param name="retall">bool, optional
        /// Set to True to return list of solutions at each iteration.
        /// Available in Results object's mle_retvals attribute.</param>
        /// <param name="skip_hessian">bool, optional
        /// If False (default), then the negative inverse hessian is calculated
        /// after the optimization. If True, then the hessian will not be
        /// calculated.</param>
        /// <param name="callback">callable callback(xk), optional
        /// Called after each iteration, as callback(xk), where xk is the
        /// current parameter vector.</param>
        /// <param name="fargs">
        /// </param>
        /// <param name="kwargs">
        /// All kwargs are passed to the chosen solver with one exception. The
        /// following keyword controls what happens after the fit::
        ///      warn_convergence : bool, optional
        ///      If True, checks the model for the converged flag. If the
        ///      converged flag is False, a ConvergenceWarning is issued.</param>
        public LikelihoodModelResults fit(
            double[] start_params=null,
            string method="newton",
            int maxiter=100,
            bool full_output=true,
            bool disp=true,
            object fargs=null,
            Delegate callback=null,
            bool retall=false,
            bool skip_hessian=false,
            Dictionary<string,object> kwargs=null)
        {
            var optimizer = new Optimizer();

            return new LikelihoodModelResults(this, null);
        }
    }
}
