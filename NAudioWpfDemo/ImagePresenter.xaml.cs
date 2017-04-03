using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NAudioWpfDemo
{
    /// <summary>
    /// Interaction logic for ImagePresenter.xaml
    /// </summary>
    public partial class ImagePresenter : Window
    {
        public string ImagePath { get; set; }
        public ImagePresenter(string path)
        {
            InitializeComponent();
            ImagePath = path;
        }

        public void ChangeImagePath(string path)
        {
            ImagePath = path;
            img.Source = new BitmapImage(new Uri(ImagePath, UriKind.Absolute));
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeImagePath(ImagePath);
        }
    }
}
