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
    /// result.xaml 的互動邏輯
    /// </summary>
    public partial class result : Window
    {
        public result(int mosaic_size, int color_diff, int min_area, int banner_h, int toolbar_h)
        {
            InitializeComponent();
            Program.SetParameters(mosaic_size, color_diff, min_area, banner_h, toolbar_h);
            Program.MainProcess();
            //@"C:\Users\dcslab\Documents\Visual Studio 2015\Projects\AOI_Tool\test10.avi"
            //mediaElement.Source = new Uri(video);
            //mediaElement.Play();
        }
        // Play the media.
        void OnMouseDownPlayMedia(object sender, RoutedEventArgs e)
        {

            // The Play method will begin the media if it is not currently active or 
            // resume media if it is paused. This has no effect if the media is
            // already running.
            mediaElement.Play();

            // Initialize the MediaElement property values.
            InitializePropertyValues();

        }

        // Pause the media.
        void OnMouseDownPauseMedia(object sender, RoutedEventArgs e)
        {

            // The Pause method pauses the media if it is currently running.
            // The Play method can be used to resume.
            mediaElement.Pause();

        }

        // Stop the media.
        void OnMouseDownStopMedia(object sender, RoutedEventArgs e)
        {

            // The Stop method stops and resets the media to be played from
            // the beginning.
            mediaElement.Stop();

        }

        // Change the volume of the media.
        private void ChangeMediaVolume(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            mediaElement.Volume = (double)volumeSlider.Value;
        }

        // Change the speed of the media.
        private void ChangeMediaSpeedRatio(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            mediaElement.SpeedRatio = (double)speedRatioSlider.Value;
        }

        // When the media opens, initialize the "Seek To" slider maximum value
        // to the total number of miliseconds in the length of the media clip.
        private void Element_MediaOpened(object sender, EventArgs e)
        {
            //timelineSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds;
        }

        // When the media playback is finished. Stop() the media to seek to media start.
        private void Element_MediaEnded(object sender, EventArgs e)
        {
            mediaElement.Stop();
        }

        // Jump to different parts of the media (seek to). 
        private void SeekToMediaPosition(object sender, RoutedPropertyChangedEventArgs<double> args)
        {
            int SliderValue = (int)timelineSlider.Value;

            // Overloaded constructor takes the arguments days, hours, minutes, seconds, miniseconds.
            // Create a TimeSpan with miliseconds equal to the slider value.
            TimeSpan ts = new TimeSpan(0, 0, 0, 0, SliderValue);
            mediaElement.Position = ts;
        }

        void InitializePropertyValues()
        {
            // Set the media's starting Volume and SpeedRatio to the current value of the
            // their respective slider controls.
            mediaElement.Volume = (double)volumeSlider.Value;
            mediaElement.SpeedRatio = (double)speedRatioSlider.Value;
        }
    }
}
