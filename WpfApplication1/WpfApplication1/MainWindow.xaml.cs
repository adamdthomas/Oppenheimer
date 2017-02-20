using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Oppenheimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<CheckedListItem<application>> applications { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            loadList();
        }


        public void loadList()
        {
            applications = new ObservableCollection<CheckedListItem<application>>();

            applications.Add(new CheckedListItem<application>(new application() { name = "Chrome Browser", imagename = "chrome" }));
            applications.Add(new CheckedListItem<application>(new application() { name = "Chrome Driver", imagename = "chromedriver" }));
            applications.Add(new CheckedListItem<application>(new application() { name = "Notepad", imagename = "notepad" }));

            DataContext = this;
        }

        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnKill_Click(object sender, RoutedEventArgs e)
        {
           

            foreach (var app in applications)
            {
                if (app.IsChecked)
                {
                    LittleBoy.Boom(app.Item.imagename);
                }
            }

           // applications.ElementAt(0).IsChecked = true;
            string n = "";
        }
    }
}
