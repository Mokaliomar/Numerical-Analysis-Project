using org.mariuszgromada.math.mxparser;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Expression = org.mariuszgromada.math.mxparser.Expression;

namespace Numerical_Project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // This is the list that will hold all our iteration data
        // ==========================================
        // 1. OBSERVABLE COLLECTIONS FOR EACH METHOD
        // ==========================================
        ObservableCollection<BracketRow> bisectionResults = new ObservableCollection<BracketRow>();
        ObservableCollection<BracketRow> falsePositionResults = new ObservableCollection<BracketRow>();
        ObservableCollection<FixedPointRow> fixedPointResults = new ObservableCollection<FixedPointRow>();
        ObservableCollection<NewtonRow> newtonResults = new ObservableCollection<NewtonRow>();
        ObservableCollection<SecantRow> secantResults = new ObservableCollection<SecantRow>();
        public MainWindow()
        {
            InitializeComponent();
            License.iConfirmNonCommercialUse("Omar");

            // Tell the DataGrid to look at this list for its data
            //dgResults.ItemsSource = iterationResults;
        }
        // Substituation of X
        public double EvaluateMath(string equation, double xValue)
        {
            string cleanEquation = equation.ToLower();

            Argument x = new Argument("x", xValue); // Argument('var_name', the_value);
            Expression exp = new Expression(cleanEquation, x); // Expression(the_equation);

            if (exp.checkSyntax() == false)
            {
                MessageBox.Show($"Error in equation {exp.getErrorMessage}");
                return double.NaN;
            }
            return exp.calculate();
        }

        private double EvaluateDerivative(string equation, double xValue)
        {
            string cleanEquation = equation.ToLower();
            // mXparser derivative syntax: der(equation, variable, value)
            string derivativeExpression = $"der({cleanEquation}, x, {xValue})";
            Expression exp = new Expression(derivativeExpression);

            if (!exp.checkSyntax())
            {
                MessageBox.Show($"Syntax Error in derivative: {exp.getErrorMessage()}");
                return double.NaN;
            }
            return exp.calculate();
        }

        #region Bisection Method
        private void Bisection_Click(object sender, RoutedEventArgs e)
        {
            bisectionResults.Clear();
            dgResults.ItemsSource = bisectionResults;

            string eq = txtBi_Equation.Text;
            if (!double.TryParse(txtBisectXl.Text, out double xl) ||
                !double.TryParse(txtBisectXu.Text, out double xu) ||
                !double.TryParse(txtBisectEa.Text, out double targetEa))
            {
                MessageBox.Show("Please enter valid numbers."); return;
            }

            double f_xl = EvaluateMath(eq, xl);
            double f_xu = EvaluateMath(eq, xu);

            if (f_xl * f_xu > 0)
            {
                MessageBox.Show("No root bracketed. f(Xl) and f(Xu) must have opposite signs."); return;
            }

            double error = 100, xr = 0, xr_old = 0;
            int counter = 1;

            while (error > targetEa && counter <= 100)
            {
                xr_old = xr;
                xr = (xl + xu) / 2;
                double f_xr = EvaluateMath(eq, xr);

                if (double.IsNaN(f_xr)) return;

                if (counter > 1 && xr != 0)
                    error = Math.Abs((xr - xr_old) / xr) * 100;

                bisectionResults.Add(new BracketRow
                {
                    Iteration = counter,
                    Xl = Math.Round(xl, 5),
                    F_Xl = Math.Round(f_xl, 5),
                    Xu = Math.Round(xu, 5),
                    F_Xu = Math.Round(f_xu, 5),
                    Xr = Math.Round(xr, 5),
                    F_Xr = Math.Round(f_xr, 5),
                    ErrorPercent = counter == 1 ? 100 : Math.Round(error, 4)
                });

                if (error <= targetEa || f_xr == 0)
                {
                    MessageBox.Show($"Root found: {Math.Round(xr, 5)} at iteration {counter}");
                    break;
                }

                // Determine which sub-interval contains the root
                if (f_xl * f_xr < 0)
                {
                    xu = xr;
                    f_xu = f_xr;
                }
                else
                {
                    xl = xr;
                    f_xl = f_xr;
                }
                counter++;
            }
        }
        #endregion

        #region False Position
        private void FalsePosition_Click(object sender, RoutedEventArgs e)
        {
            falsePositionResults.Clear();
            dgResults.ItemsSource = falsePositionResults;

            string eq = txtFalse_Equation.Text;
            if (!double.TryParse(txtFPXl.Text, out double xl) ||
                !double.TryParse(txtFPXu.Text, out double xu) ||
                !double.TryParse(txtFPEa.Text, out double targetEa))
            {
                MessageBox.Show("Please enter valid numbers."); return;
            }

            double f_xl = EvaluateMath(eq, xl);
            double f_xu = EvaluateMath(eq, xu);

            if (f_xl * f_xu > 0)
            {
                MessageBox.Show("No root bracketed. f(Xl) and f(Xu) must have opposite signs."); return;
            }

            double error = 100, xr = 0, xr_old = 0;
            int counter = 1;

            while (error > targetEa && counter <= 100)
            {
                xr_old = xr;
                // False Position Formula
                xr = xu - (f_xu * (xl - xu)) / (f_xl - f_xu);
                double f_xr = EvaluateMath(eq, xr);

                if (double.IsNaN(f_xr)) return;

                if (counter > 1 && xr != 0)
                    error = Math.Abs((xr - xr_old) / xr) * 100;

                falsePositionResults.Add(new BracketRow
                {
                    Iteration = counter,
                    Xl = Math.Round(xl, 5),
                    F_Xl = Math.Round(f_xl, 5),
                    Xu = Math.Round(xu, 5),
                    F_Xu = Math.Round(f_xu, 5),
                    Xr = Math.Round(xr, 5),
                    F_Xr = Math.Round(f_xr, 5),
                    ErrorPercent = counter == 1 ? 100 : Math.Round(error, 4)
                });

                if (error <= targetEa || f_xr == 0)
                {
                    MessageBox.Show($"Root found: {Math.Round(xr, 5)} at iteration {counter}");
                    break;
                }

                if (f_xl * f_xr < 0)
                {
                    xu = xr;
                    f_xu = f_xr;
                }
                else
                {
                    xl = xr;
                    f_xl = f_xr;
                }
                counter++;
            }
        }
        #endregion

        #region Fixed_Point
        private void FixedPoint_Click(object sender, RoutedEventArgs e)
        {
            // Clear the table from any previous calculations
            fixedPointResults.Clear();
            dgResults.ItemsSource = fixedPointResults;

            // 1. Get the inputs
            string g_x = txtG_x.Text; // Use the new TextBox!

            // Validate inputs
            if (!double.TryParse(txtFixedX0.Text, out double xi) ||
                !double.TryParse(txtFixedEa.Text, out double targetEa))
            {
                MessageBox.Show("Please enter valid numbers for X0 and Ea.");
                return;
            }

            // 2. Setup starting variables
            double error = 100;
            int counter = 1;
            double xi_1 = 0;

            // 3. The Loop
            while (error > targetEa)
            {
                // Calculate the next X
                xi_1 = EvaluateMath(g_x, xi);

                // Check if the math parser returned NaN (e.g., syntax error in string)
                if (double.IsNaN(xi_1))
                {
                    return; // EvaluateMath already shows the error message, so just stop the loop.
                }

                // Calculate Error
                if (counter > 1 && xi_1 != 0)
                {
                    error = Math.Abs((xi_1 - xi) / xi_1) * 100;
                }

                // 4. Add the data to our Table
                fixedPointResults.Add(new FixedPointRow
                {
                    Iteration = counter,
                    Xi = Math.Round(xi, 5),
                    Xi_1 = Math.Round(xi_1, 5),
                    ErrorPercent = (counter == 1) ? 100 : error // Show 100% error for the first step
                });

                // 5. Check stop condition
                if (error <= targetEa)
                {
                    MessageBox.Show($"Root found: {xi_1} at iteration {counter}");
                    break;
                }

                // 6. Update for next loop
                xi = xi_1;
                counter++;

                // Failsafe
                if (counter > 100)
                {
                    MessageBox.Show("Divergence detected. Reached 100 iterations without finding a root.");
                    break;
                }
            }
        }

        #endregion

        #region Newton Rapshon
        private void Newton_Click(object sender, RoutedEventArgs e)
        {
            newtonResults.Clear();
            dgResults.ItemsSource = newtonResults;

            string eq = txtNewtonEquation.Text;
            if (!double.TryParse(txtNewtonX0.Text, out double xi) ||
                !double.TryParse(txtNewtonEa.Text, out double targetEa))
            {
                MessageBox.Show("Please enter valid numbers."); return;
            }

            double error = 100, xi_1 = 0;
            int counter = 1;

            while (error > targetEa && counter <= 100)
            {
                double f_xi = EvaluateMath(eq, xi);
                double f_prime_xi = EvaluateDerivative(eq, xi);

                if (double.IsNaN(f_xi) || double.IsNaN(f_prime_xi)) return;

                if (f_prime_xi == 0)
                {
                    MessageBox.Show("Derivative is zero. Division by zero error."); return;
                }

                // Newton-Raphson Formula
                xi_1 = xi - (f_xi / f_prime_xi);

                if (xi_1 != 0)
                    error = Math.Abs((xi_1 - xi) / xi_1) * 100;

                newtonResults.Add(new NewtonRow
                {
                    Iteration = counter,
                    Xi = Math.Round(xi, 5),
                    Xi_plus_1 = Math.Round(xi_1, 5),
                    F_Xi = Math.Round(f_xi, 5),
                    F_Prime_Xi = Math.Round(f_prime_xi, 5),
                    ErrorPercent = counter == 1 ? 100 : Math.Round(error, 4)
                });

                if (error <= targetEa)
                {
                    MessageBox.Show($"Root found: {Math.Round(xi_1, 5)} at iteration {counter}");
                    break;
                }
                xi = xi_1;
                counter++;
            }
        }
        #endregion

        #region Secant Method
        private void Secant_Click(object sender, RoutedEventArgs e)
        {
            secantResults.Clear();
            dgResults.ItemsSource = secantResults;

            string eq = txtSecant_Equation.Text;
            if (!double.TryParse(txtSecantX_1.Text, out double xi_minus_1) ||
                !double.TryParse(txtSecantX0.Text, out double xi) ||
                !double.TryParse(txtSecantEa.Text, out double targetEa))
            {
                MessageBox.Show("Please enter valid numbers."); return;
            }

            double error = 100, xi_plus_1 = 0;
            int counter = 1;

            while (error > targetEa && counter <= 100)
            {
                double f_xi_minus_1 = EvaluateMath(eq, xi_minus_1);
                double f_xi = EvaluateMath(eq, xi);

                if (double.IsNaN(f_xi_minus_1) || double.IsNaN(f_xi)) return;

                if (f_xi_minus_1 - f_xi == 0)
                {
                    MessageBox.Show("Division by zero error in Secant calculation."); return;
                }

                // Secant Formula
                xi_plus_1 = xi - (f_xi * (xi_minus_1 - xi)) / (f_xi_minus_1 - f_xi);

                if (counter != 1)
                    error = Math.Abs((xi - xi_minus_1) / xi) * 100;

                secantResults.Add(new SecantRow
                {
                    Iteration = counter,
                    Xi_minus_1 = Math.Round(xi_minus_1, 5),
                    Xi = Math.Round(xi, 5),
                    F_Xi_minus_1 = Math.Round(f_xi_minus_1, 5),
                    F_Xi = Math.Round(f_xi, 5),
                    //Xi_plus_1 = Math.Round(xi_plus_1, 5),
                    ErrorPercent = counter == 1 ? 100 : Math.Round(error, 4)
                });

                if (error <= targetEa)
                {
                    MessageBox.Show($"Root found: {Math.Round(xi, 5)} at iteration {counter}");
                    break;
                }

                // Shift values for the next iteration
                xi_minus_1 = xi;
                xi = xi_plus_1;
                counter++;
            }
        #endregion

        }
        // For Bisection and False Position (they use the same variables!)
        public class BracketRow
        {
            public int Iteration { get; set; }
            public double Xl { get; set; }
            public double F_Xl { get; set; }
            public double Xu { get; set; }
            public double F_Xu { get; set; }
            public double Xr { get; set; }
            public double F_Xr { get; set; } // Shows the function value at the root
            public double ErrorPercent { get; set; }
        }
        public class FixedPointRow
        {
            public int Iteration { get; set; }
            public double Xi { get; set; }
            public double Xi_1 { get; set; }
            public double ErrorPercent { get; set; }
        }

        // For Newton-Raphson
        public class NewtonRow
        {
            public int Iteration { get; set; }
            public double Xi { get; set; }
            public double Xi_plus_1 { get; set; }
            public double F_Xi { get; set; }
            public double F_Prime_Xi { get; set; }
            public double ErrorPercent { get; set; }
        }

        // For Secant
        public class SecantRow
        {
            public int Iteration { get; set; }
            public double Xi_minus_1 { get; set; }
            public double F_Xi_minus_1 { get; set; }
            public double Xi { get; set; }
            public double F_Xi { get; set; }
            //public double Xi_plus_1 { get; set; }
            public double ErrorPercent { get; set; }
        }
    }
}