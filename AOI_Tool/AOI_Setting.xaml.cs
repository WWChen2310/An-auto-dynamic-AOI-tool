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

namespace AOI_Tool
{
    /// <summary>
    /// AOI_Setting.xaml 的互動邏輯
    /// </summary>
    public partial class AOI_Setting : Window
    {
        public AOI_Setting(string video, string fixation)
        {
            InitializeComponent();
            //Program.MainProcess();
            //this.video = video.Replace("\\", "\\\\");
            //this.fixation = fixation.Replace("\\", "\\\\");

            Program.LoadVideo(video);
            Program.LoadData(fixation);
            //Program.MainProcess();
        }

        private void ok_Click(object sender, RoutedEventArgs e)
        { 
            new result((int)mosaic_size.Value, (int)color_diff.Value, (int)min_area.Value, (int)banner_h.Value, (int)toolbar_h.Value).Show();
        }
    }
}
