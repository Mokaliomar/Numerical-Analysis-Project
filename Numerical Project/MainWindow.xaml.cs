using org.mariuszgromada.math.mxparser;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Security.AccessControl;
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

        #region Helper Methods
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
        // Derivative of X
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
        // Determinant Evaluation !
        private double EvaluateDeterminant(double[,] m)
        {
            // فك المحدد الثلاثي في سطر برمجي واحد متقسم على 3 أسطر عشان سهولة القراءة
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
                 - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
                 + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        }
        private double GetModifiedDeterminant(double[,] originalA, double[] B, int replaceCol)
        {
            // 1. نعمل مصفوفة جديدة فاضية
            double[,] tempA = new double[3, 3];

            // 2. ننسخ كل الأرقام من A الأصلية لـ tempA عشان نحمي الأصلية
            Array.Copy(originalA, tempA, originalA.Length);

            // 3. نبدل العمود المطلوب بعمود النواتج B
            for (int i = 0; i < 3; i++)
            {
                tempA[i, replaceCol] = B[i];
            }

            // 4. نحسب المحدد للمصفوفة الجديدة بعد التعديل ونرجعه
            return EvaluateDeterminant(tempA);
        }

        // دالة إيجاد الصف اللي فيه أكبر رقم (بناءً على القيمة المطلقة)
        private int GetMaxRowIndex(double[,] A, int currentStep)
        {
            int maxRow = currentStep;
            double maxValue = Math.Abs(A[currentStep, currentStep]);

            // بنبدأ تدوير من الصف اللي تحت الـ Pivot الحالي علطول
            for (int i = currentStep + 1; i < 3; i++)
            {
                if (Math.Abs(A[i, currentStep]) > maxValue)
                {
                    maxValue = Math.Abs(A[i, currentStep]);
                    maxRow = i;
                }
            }
            return maxRow;
        }

        // دالة التبديل بين صفين
        private void SwapRows(double[,] A, int row1, int row2)
        {
            // لو هو نفس الصف، مفيش داعي نضيع وقت في التبديل
            if (row1 == row2) return;

            for (int i = 0; i < 4; i++)
            {
                double temp = A[row1, i];
                A[row1, i] = A[row2, i];
                A[row2, i] = temp;
            }
        }

        #endregion

        #region Control the Tab appearance 
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // السطر ده عشان نتأكد إن الواجهة حملت كلها ومفيش حاجة بـ null وتعمل Crash
            if (dgResults == null || tabCramer == null || tabGauss == null || tabLU == null) return;

            // بنسأل: هل التاب بتاعة Cramer هي اللي مفتوحة دلوقتي؟
            if (tabCramer.IsSelected || tabGauss.IsSelected || tabLU.IsSelected)
            {
                // لو اه، اخفي الـ DataGrid تماماً (Collapsed بتخفيها وتلغي المساحة الفاضية بتاعتها)
                dgResults.Visibility = Visibility.Collapsed;

                // 2. قول للدور التاني (التابات): "إنت بقيت النجم (*)"، افرد نفسك وخد كل الشاشة اللي فاضية
                TabRow.Height = new GridLength(1, GridUnitType.Star);

                // 3. قول للدور التالت (الجدول): "إنت خلاص مبقاش ليك لازمة (Auto)"، صغر نفسك ومتاخدش مساحة
                GridRow.Height = GridLength.Auto;
            }
            else
            {
                // لو أي تاب تانية، رجع الـ DataGrid تظهر تاني
                dgResults.Visibility = Visibility.Visible;

                // 2. قول للدور التاني (التابات): "إنت بقيت النجم (*)"، افرد نفسك وخد كل الشاشة اللي فاضية
                TabRow.Height = GridLength.Auto;

                // 3. قول للدور التالت (الجدول): "إنت خلاص مبقاش ليك لازمة (Auto)"، صغر نفسك ومتاخدش مساحة
                GridRow.Height = new GridLength(1, GridUnitType.Star);
            }
        }
        #endregion

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

        }
        #endregion

        #region Gauss Elimination
        private void Gauss_Click(object sender, RoutedEventArgs e)
        {
            double[,] A = new double[3,4];
            // بنستخدم Try و Catch عشان لو اليوزر دخل حروف بدل أرقام نطلعله رسالة بدل ما البرنامج يكراش
            try
            {
                // --- سحب بيانات الصف الأول ---
                A[0, 0] = Convert.ToDouble(ga11.Text);
                A[0, 1] = Convert.ToDouble(ga12.Text);
                A[0, 2] = Convert.ToDouble(ga13.Text);
                A[0, 3] = Convert.ToDouble(gb1.Text);

                // --- سحب بيانات الصف الثاني ---
                A[1, 0] = Convert.ToDouble(ga21.Text);
                A[1, 1] = Convert.ToDouble(ga22.Text);
                A[1, 2] = Convert.ToDouble(ga23.Text);
                A[1, 3] = Convert.ToDouble(gb2.Text);

                // --- سحب بيانات الصف الثالث ---
                A[2, 0] = Convert.ToDouble(ga31.Text);
                A[2, 1] = Convert.ToDouble(ga32.Text);
                A[2, 2] = Convert.ToDouble(ga33.Text);
                A[2, 3] = Convert.ToDouble(gb3.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please make sure all boxes are filled with valid numbers!");
                return; // بنوقف الكود هنا عشان ميكملش حسابات بأرقام بايظة
            }

            // --- حماية من القسمة على صفر للـ Pivot الأول ---
            if (A[0, 0] == 0)
            {
                MessageBox.Show("A[0,0] is zero! Cannot proceed without Partial Pivoting. Please rearrange your equations.");
                return;
            }

            // المتغير بتاعنا اللي بيشوف الـ Checkbox متعلم ولا لأ
            bool usePivoting = chkPartialPivot.IsChecked == true;

            // --- Step 1: Forward Elimination للعمود الأول (Pivot A[0,0]) ---
            if (usePivoting)
            {
                int maxRow = GetMaxRowIndex(A, 0);
                SwapRows(A, 0, maxRow);
            }

            // Forward Elimination (Step 2)
            double m21 = A[1, 0] / A[0, 0];
            for(int i = 0; i < 4; i++)
            {
                A[1, i] -= m21 * A[0, i];
            }

            double m31 = A[2, 0] / A[0, 0];
            for (int i = 0; i < 4; i++)
            {
                A[2, i] -= m31 * A[0, i];
            }

            // --- Step 2: Forward Elimination للعمود التاني (Pivot A[1,1]) ---
            if (usePivoting)
            {
                int maxRow = GetMaxRowIndex(A, 1);
                SwapRows(A, 1, maxRow);
            }

            // --- حماية من القسمة على صفر للـ Pivot التاني ---
            if (A[1, 1] == 0)
            {
                MessageBox.Show("A[1,1] became zero! Cannot proceed.");
                return;
            }

            double m32 = A[2, 1] / A[1, 1];
            for (int i = 0; i < 4; i++)
            {
                A[2, i] -= m32 * A[1, i];
            }

            // Backward Substitution (Calculate the X values)
            double X3 = Math.Round(A[2, 3] / A[2, 2], 5);

            double X2 = Math.Round((A[1, 3] - A[1, 2] * X3) / A[1, 1], 5);

            double X1 = Math.Round((A[0, 3] - A[0, 2] * X3 - A[0, 1] * X2) / A[0, 0], 5);

            MessageBox.Show($"X1 = {X1}\nX2 = {X2}\nX3 = {X3}");
        }
        #endregion

        #region LU Decomposition
        // Helper Method, علشان افصل المصفوفه A وتبقي بعد كده سهله في التعويض بعدها
        private void GetLUDecomposition(double[,] A, out double[,] L, out double[,] U)
        {
            // 1. تعريف مصفوفة L (ونحط على القطر الرئيسي وحايد)
            L = new double[3, 3];
            for (int i = 0; i < 3; i++)
                L[i, i] = 1.0;

            // 2. تعريف مصفوفة U (في البداية بتكون نسخة من A)
            U = new double[3, 3];
            Array.Copy(A, U, A.Length);

            // 3. تطبيق الـ Forward Elimination على U، وتخزين الـ m في L

            // --- Step 1 ---
            if (U[0, 0] == 0) throw new DivideByZeroException("Pivot U[0,0] is zero.");

            double m21 = U[1, 0] / U[0, 0];
            L[1, 0] = m21; // بنخزن الـ Multiplier في L
            for (int i = 0; i < 3; i++) U[1, i] -= m21 * U[0, i]; // بنحدث U

            double m31 = U[2, 0] / U[0, 0];
            L[2, 0] = m31;
            for (int i = 0; i < 3; i++) U[2, i] -= m31 * U[0, i];

            // --- Step 2 ---
            if (U[1, 1] == 0) throw new DivideByZeroException("Pivot U[1,1] is zero.");

            double m32 = U[2, 1] / U[1, 1];
            L[2, 1] = m32;
            for (int i = 0; i < 3; i++) U[2, i] -= m32 * U[1, i];
        }
        private void LU_Click(object sender, RoutedEventArgs e)
        {
            double[,] A = new double[3, 3];
            double[] B = new double[3];
            // بنستخدم Try و Catch عشان لو اليوزر دخل حروف بدل أرقام نطلعله رسالة بدل ما البرنامج يكراش
            try
            {
                // --- سحب بيانات الصف الأول ---
                A[0, 0] = Convert.ToDouble(lu_a11.Text);
                A[0, 1] = Convert.ToDouble(lu_a12.Text);
                A[0, 2] = Convert.ToDouble(lu_a13.Text);
                B[0] = Convert.ToDouble(lu_b1.Text);

                // --- سحب بيانات الصف الثاني ---
                A[1, 0] = Convert.ToDouble(lu_a21.Text);
                A[1, 1] = Convert.ToDouble(lu_a22.Text);
                A[1, 2] = Convert.ToDouble(lu_a23.Text);
                B[1] = Convert.ToDouble(lu_b2.Text);

                // --- سحب بيانات الصف الثالث ---
                A[2, 0] = Convert.ToDouble(lu_a31.Text);
                A[2, 1] = Convert.ToDouble(lu_a32.Text);
                A[2, 2] = Convert.ToDouble(lu_a33.Text);
                B[2] = Convert.ToDouble(lu_b3.Text);

                GetLUDecomposition(A, out double[,] L, out double[,] U);

                // Forward Substitution (L * Y = B)
                double[] Y = new double[3];
                Y[0] = B[0];
                Y[1] = B[1] - (L[1, 0] * Y[0]);
                Y[2] = B[2] - (L[2, 0] * Y[0]) - (L[2, 1] * Y[1]);

                // Backward Substitution (U * X = Y)
                double X1, X2, X3;
                X3 = Math.Round(Y[2] / U[2, 2], 5);
                X2 = Math.Round((Y[1] - (U[1, 2] * X3)) / U[1, 1], 5);
                X1 = Math.Round((Y[0] - (U[0, 1] * X2) - (U[0, 2] * X3)) / U[0, 0], 5);

                MessageBox.Show($"X1 = {X1}\nX2 = {X2}\nX3 = {X3}");

            }
            catch (FormatException)
            {
                MessageBox.Show("Please make sure all boxes are filled with valid numbers!");
                return; // بنوقف الكود هنا عشان ميكملش حسابات بأرقام بايظة
            }
            catch (Exception ex)
            {
                // ده هيمسك القسمة على صفر أو أي مشكلة من دالة الـ LU
                MessageBox.Show($"Mathematical Error: {ex.Message}");
                return;
            }

        }
        #endregion

        #region Crammer's Rule
        private void Crammer_Click(object sender, RoutedEventArgs e)
        {
            // 1. تعريف مصفوفة المعاملات (3 صفوف و 3 أعمدة)
            double[,] A = new double[3, 3];

            // 2. تعريف مصفوفة النواتج (3 أماكن)
            double[] B = new double[3];

            // بنستخدم Try و Catch عشان لو اليوزر دخل حروف بدل أرقام نطلعله رسالة بدل ما البرنامج يكراش
            try
            {
                // --- سحب بيانات الصف الأول ---
                A[0, 0] = Convert.ToDouble(a11.Text);
                A[0, 1] = Convert.ToDouble(a12.Text);
                A[0, 2] = Convert.ToDouble(a13.Text);
                B[0] = Convert.ToDouble(b1.Text);

                // --- سحب بيانات الصف الثاني ---
                A[1, 0] = Convert.ToDouble(a21.Text);
                A[1, 1] = Convert.ToDouble(a22.Text);
                A[1, 2] = Convert.ToDouble(a23.Text);
                B[1] = Convert.ToDouble(b2.Text);

                // --- سحب بيانات الصف الثالث ---
                A[2, 0] = Convert.ToDouble(a31.Text);
                A[2, 1] = Convert.ToDouble(a32.Text);
                A[2, 2] = Convert.ToDouble(a33.Text);
                B[2] = Convert.ToDouble(b3.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please make sure all boxes are filled with valid numbers!");
                return; // بنوقف الكود هنا عشان ميكملش حسابات بأرقام بايظة
            }

            double D = EvaluateDeterminant(A);

            if (D == 0)
            {
                MessageBox.Show("The main determinant (D) is 0. This system has no unique solution.");
                return; // بنوقف الكود عشان مانعملش قسمة على صفر
            }

            // عشان نجيب المحددات الفرعية في سطر واحد لكل واحدة!
            // لاحظ إن الـ Index بيبدأ من 0 (يعني العمود الأول 0، التاني 1، التالت 2)
            double D1 = GetModifiedDeterminant(A, B, 0);
            double D2 = GetModifiedDeterminant(A, B, 1);
            double D3 = GetModifiedDeterminant(A, B, 2);

            // حساب النواتج النهائية
            double X1 = Math.Round(D1 / D, 5);
            double X2 = Math.Round(D2 / D, 5);
            double X3 = Math.Round(D3 / D, 5);

            // ممكن هنا تعرضهم في MessageBox أو Label في الواجهة زي ما تحب
            MessageBox.Show($"X1 = {X1}\nX2 = {X2}\nX3 = {X3}");
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