using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Maxci.Runner.Views
{
    /// <summary>
    /// Interaction logic for RunnerView.xaml
    /// </summary>
    public partial class RunnerView : Window
    {
        public RunnerView()
        {
            InitializeComponent();
        }

        private void LoaderView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
