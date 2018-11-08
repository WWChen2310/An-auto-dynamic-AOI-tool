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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;


namespace AOI_Tool
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // video_type
            this.video_type.Items.Add("Webpage");
            this.video_type.Items.Add("Teaching");
            this.video_type.SelectedIndex = 0;
        }

        private void video_browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.video_filename.Text = openFileDialog.FileName;
            
        }

        private void fixation_browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.fixation_filename.Text = openFileDialog.FileName;
        }
        private void ok_Click(object sender, RoutedEventArgs e)
        {
            //Program.MainProcess();
            new AOI_Setting(video_filename.Text, fixation_filename.Text).Show();
        }

        #region 

        #endregion
    }
}
