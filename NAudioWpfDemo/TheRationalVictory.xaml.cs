

using NAudio.Dsp;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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

namespace NAudioWpfDemo
{
    public static class Extensions
    {
        public static bool IsNaN(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            double x;
            return !double.TryParse(str, out x);
        }

        public static bool IsNaN(this TextBox txt)
        {
            var str = txt.Text;
            return str.IsNaN();
        }
    }

    public class SamplesDir
    {
        public SamplesDir()
        {
            SubDirs = new List<SamplesDir>();
            Files = new List<Sample>();
        }

        public int Order { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }

        public List<SamplesDir> SubDirs { get; set; }

        public List<Sample> Files { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Sample
    {
        public int Order { get; set; }

        public string Name { get; set; }
        public string WavPath { get; set; }
        public string SettingsPath { get; set; }
        public string ImagePath { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }


    [DataContract]
    public class Settings
    {
        [DataMember]
        public string WavFilePath { get; set; }
        [DataMember]
        public float Contrast { get; set; }
        [DataMember]
        public int BmpSize { get; set; }
        [DataMember]
        public float BluePower { get; set; }
        [DataMember]
        public float GreenPower { get; set; }
        [DataMember]
        public float RedPower { get; set; }
        [DataMember]
        public int WindowSize { get; set; }
        [DataMember]
        public string ExportSettingsPath { get; set; }
        [DataMember]
        public string ImportSettingsPath { get; set; }



        [DataMember]
        public int MaxBlue { get; set; }

        [DataMember]
        public int MaxGreen { get; set; }

        [DataMember]
        public int MaxRed { get; set; }

        [DataMember]
        public int MinBlue { get; set; }

        [DataMember]
        public int MinGreen { get; set; }

        [DataMember]
        public int MinRed { get; set; }

        [DataMember]
        public double MinMag { get; set; }


        [DataMember]
        public double MaxMag { get; set; }

        [DataMember]
        public bool Reversed { get; set; }

        [DataMember]
        public bool UseMagRange { get; set; }
    }

    /// <summary>
    /// Interaction logic for Nadav.xaml
    /// </summary>
    public partial class Nadav : Window, IDisposable
    {
        private ObservableCollection<SamplesDir> Samples { get; set; }
        private ImagePresenter ImgWin { get; set; }

        public Nadav()
        {
            InitializeComponent();

            ReadSamples();

        }

        private void ReadSamples()
        {
            try
            {
                Samples = new ObservableCollection<SamplesDir>();
                var dirs = new DirectoryInfo("Samples");
                foreach (var d in dirs.GetDirectories().OrderBy(x => x.Name))
                {
                    var samplesDir = new SamplesDir()
                    {
                        Name = d.Name,
                        Path = d.FullName
                    };
                    Samples.Add(samplesDir);


                    var subDirsHaveOrder = false;
                    foreach (var subDir in d.GetDirectories().OrderBy(x => x.Name))
                    {
                        var subSamplesDir = new SamplesDir()
                        {
                            Name = subDir.Name,
                            Path = subDir.FullName
                        };

                        int order;
                        var splitted = subDir.Name.Split('_');
                        if (subDir.Name.Contains('_') && int.TryParse(splitted[0], out order))
                        {
                            subSamplesDir.Order = order;
                            subDirsHaveOrder = true;
                        }

                        samplesDir.SubDirs.Add(subSamplesDir);

                        var files = subDir.GetFiles();

                        var filesOrdered = false;

                        foreach (var f in files.Where(x => x.Name.ToLower().EndsWith(".wav")).OrderBy(x => x.Name))
                        {
                            var settingsFile = files.FirstOrDefault(x => x.Name.ToLower() == f.Name.ToLower().Replace(".wav", ".xml"));
                            if (settingsFile == null)
                                continue;

                            var imgFile = files.FirstOrDefault(x => x.Name.ToLower() == f.Name.ToLower().Replace(".wav", ".png"));
                            if (imgFile == null)
                            {
                                imgFile = files.FirstOrDefault(x => x.Name.ToLower() == f.Name.ToLower().Replace(".wav", ".jpg"));
                                if (imgFile == null)
                                    continue;
                            }


                            int fOrder;

                            // Take the name, then take the number (if they are ordered)
                            var name = f.Name.Split('.')[0];
                            name = name.Split('-')[0];


                            var sample = new Sample()
                            {
                                Name = f.Name.Split('.')[0],
                                WavPath = f.FullName,
                                SettingsPath = settingsFile.FullName,
                                ImagePath = imgFile.FullName
                            };

                            if (int.TryParse(name, out fOrder))
                            {
                                filesOrdered = true;
                                sample.Order = fOrder;
                            }

                            subSamplesDir.Files.Add(sample);
                        }

                        if (filesOrdered)
                            subSamplesDir.Files = subSamplesDir.Files.OrderBy(x => x.Order).ToList();
                    }

                    if (subDirsHaveOrder)
                    {
                        samplesDir.SubDirs = samplesDir.SubDirs.OrderBy(x => x.Order).ToList();
                    }

                }

                samplesDirsCmb.ItemsSource = Samples;
                samplesDirsCmb.SelectedItem = Samples.FirstOrDefault();

            }
            catch (Exception ex)
            {
                errLbl.Content = "Couldn't read samples." + Environment.NewLine + ex;
            }
        }

        public Settings CreateSettings()
        {
            var settings = new Settings();
            settings.WavFilePath = path;
            //settings.Contrast = contrast;
            settings.BmpSize = int.Parse(bmpSizeTxt.Text);
            settings.BluePower = float.Parse(blueMagnitudePowerTxt.Text);
            settings.GreenPower = float.Parse(greenMagnitudePowerTxt.Text);
            settings.RedPower = float.Parse(redMagnitudePowerTxt.Text);
            settings.WindowSize = int.Parse((windowSizeCmb.SelectedItem as ComboBoxItem).Content.ToString());
            settings.ExportSettingsPath = settingsExportPath.Text;
            settings.ImportSettingsPath = settingsImportPath.Text;
            settings.MaxBlue = int.Parse(maxBlueTxt.Text);
            settings.MaxGreen = int.Parse(maxGreenTxt.Text);
            settings.MaxRed = int.Parse(maxRedTxt.Text);
            settings.MinBlue = int.Parse(minBlueTxt.Text);
            settings.MinGreen = int.Parse(minGreenTxt.Text);
            settings.MinRed = int.Parse(minRedTxt.Text);
            settings.Reversed = reverseChk.IsChecked.HasValue && reverseChk.IsChecked.Value;

            var maxMag = double.MaxValue;
            double.TryParse(maxMagTxt.Text, out maxMag);
            settings.MaxMag = maxMag;

            var minMag = 0d;
            double.TryParse(minMagTxt.Text, out minMag);
            settings.MinMag = minMag;

            settings.UseMagRange = magRangeChk.IsChecked ?? false;

            return settings;
        }

        public void FromSettings(Settings settings, bool ignorePathes = false)
        {
            //contrastTxt.Text = settings.Contrast.ToString();
            bmpSizeTxt.Text = settings.BmpSize.ToString();
            blueMagnitudePowerTxt.Text = settings.BluePower.ToString();
            greenMagnitudePowerTxt.Text = settings.GreenPower.ToString();
            redMagnitudePowerTxt.Text = settings.RedPower.ToString();
            maxBlueTxt.Text = settings.MaxBlue.ToString();
            maxGreenTxt.Text = settings.MaxGreen.ToString();
            maxRedTxt.Text = settings.MaxRed.ToString();
            minBlueTxt.Text = settings.MinBlue.ToString();
            minGreenTxt.Text = settings.MinGreen.ToString();
            minRedTxt.Text = settings.MinRed.ToString();
            maxMagTxt.Text = settings.MaxMag.ToString("0.0#########");
            minMagTxt.Text = settings.MinMag.ToString("0.0#########");
            reverseChk.IsChecked = settings.Reversed;

            var windowSize = settings.WindowSize.ToString();
            windowSizeCmb.SelectedItem = windowSizeCmb.Items.Cast<ComboBoxItem>().Where(item => item.Content.ToString() == windowSize).SingleOrDefault();

            if (!ignorePathes)
            {
                filePathTxt.Text = settings.WavFilePath;
                settingsExportPath.Text = settings.ExportSettingsPath;
                settingsImportPath.Text = settings.ImportSettingsPath;
            }

            magRangeChk.IsChecked = settings.UseMagRange;
        }

        string path = @"C:\Users\nadavsof\Dropbox\TheRationVictoryProjects\letters\2bet.wav";

        //float contrast = 20f; // 1 for no contrast. 2 for double, etc

        int defaultfftLength = 1024;

        volatile bool ready;


        AudioPlayback player;

        private int tutorialStep = 1;
        private bool tutorialMode;
        private int lastTutorialStep;


        public List<Complex[]> AllFFT = new List<Complex[]>();

        public List<Complex[]> FFTsSlidingWindow = new List<Complex[]>();
        private void Run()
        {

            SampleAggregator.defaultfftLength = defaultfftLength;

            if (player != null)
                player.Dispose();
            player = new AudioPlayback();
            player.Load(path);
            player.FftCalculated += player_FftCalculated;
            player.playbackDevice.PlaybackStopped += playbackDevice_PlaybackStopped;
            player.Play();
            startBtn.Background = new SolidColorBrush(Colors.Yellow);
        }

        void playbackDevice_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            WorkOnCalulatedFTTs();
            player.Dispose();
            startBtn.Background = new SolidColorBrush(Colors.Green);
            ready = true;
            changeContrastBtn.IsEnabled = true;
        }


        public bool Validate()
        {
            if (!tutorialMode)
                AllBlack();

            errLbl.Content = "";
            var sb = new StringBuilder();
            if (string.IsNullOrEmpty(filePathTxt.Text))
                sb.AppendLine("Wav file path required");

            if (blueMagnitudePowerTxt.IsNaN())
            {
                sb.AppendLine("Blue magnitude is not a number");
                Red(blueMagnitudePowerTxt);
            }


            if (greenMagnitudePowerTxt.IsNaN())
            {
                sb.AppendLine("Green magnitude is not a number");
                Red(greenMagnitudePowerTxt);
            }


            if (redMagnitudePowerTxt.IsNaN())
            {
                sb.AppendLine("Red magnitude is not a number");
                Red(redMagnitudePowerTxt);
            }




            if (maxBlueTxt.IsNaN())
            {
                sb.AppendLine("Max hue of Blue is not a number");
                Red(maxBlueTxt);
            }


            if (maxGreenTxt.IsNaN())
            {
                sb.AppendLine("Max hue of Green is not a number");
                Red(maxGreenTxt);
            }


            if (maxRedTxt.IsNaN())
            {
                sb.AppendLine("Max hue of Red is not a number");
                Red(maxRedTxt);
            }




            if (minBlueTxt.IsNaN())
            {
                sb.AppendLine("Min hue of Blue is not a number");
                Red(minBlueTxt);
            }


            if (minGreenTxt.IsNaN())
            {
                sb.AppendLine("Min hue of Green is not a number");
                Red(minGreenTxt);
            }


            if (minRedTxt.IsNaN())
            {
                sb.AppendLine("Min hue of Red is not a number");
                Red(minRedTxt);
            }


            if (!string.IsNullOrEmpty(minMagTxt.Text) && minMagTxt.IsNaN())
            {
                sb.AppendLine("Min magnitude is not a number");
                Red(minMagTxt);
            }


            if (!string.IsNullOrEmpty(maxMagTxt.Text) && maxMagTxt.IsNaN())
            {
                sb.AppendLine("Max magnitude is not a number");
                Red(maxMagTxt);
            }

            //if (contrastTxt.IsNaN())
            //{
            //    sb.AppendLine("Contrast is not a number");
            //    Red(contrastTxt);
            //}

            if (bmpSizeTxt.IsNaN())
            {
                sb.AppendLine("Image size is not a number");
                Red(bmpSizeTxt);
            }

            var err = sb.ToString();
            errLbl.Content = err;

            return string.IsNullOrEmpty(err);

        }

        private void AllBlack()
        {
            filePathTxt.Foreground = new SolidColorBrush(Colors.Black);
            windowSizeCmb.Foreground = new SolidColorBrush(Colors.Black);

            blueMagnitudePowerTxt.Foreground = new SolidColorBrush(Colors.Black);

            greenMagnitudePowerTxt.Foreground = new SolidColorBrush(Colors.Black);
            redMagnitudePowerTxt.Foreground = new SolidColorBrush(Colors.Black);
            maxBlueTxt.Foreground = new SolidColorBrush(Colors.Black);


            maxGreenTxt.Foreground = new SolidColorBrush(Colors.Black);


            maxRedTxt.Foreground = new SolidColorBrush(Colors.Black);




            minBlueTxt.Foreground = new SolidColorBrush(Colors.Black);


            minGreenTxt.Foreground = new SolidColorBrush(Colors.Black);


            minRedTxt.Foreground = new SolidColorBrush(Colors.Black);


            minMagTxt.Foreground = new SolidColorBrush(Colors.Black);


            maxMagTxt.Foreground = new SolidColorBrush(Colors.Black);
            //contrastTxt.Foreground = new SolidColorBrush(Colors.Black);


            bmpSizeTxt.Foreground = new SolidColorBrush(Colors.Black);

            settingsExportPath.Foreground = new SolidColorBrush(Colors.Black);
            settingsImportPath.Foreground = new SolidColorBrush(Colors.Black);

            minPresentedMagLbl.Foreground = new SolidColorBrush(Colors.Black);
            maxPresentedMagLbl.Foreground = new SolidColorBrush(Colors.Black);
            minMagLbl.Foreground = new SolidColorBrush(Colors.Black);
            maxMagLbl.Foreground = new SolidColorBrush(Colors.Black);

        }

        private void Red(Control ctrl)
        {
            ctrl.Foreground = new SolidColorBrush(Colors.Red);
        }

        private void Blue(Control ctrl)
        {
            ctrl.Foreground = new SolidColorBrush(Colors.Blue);
        }


        //public System.Drawing.Bitmap SetContrast(System.Drawing.Bitmap originalImage)
        //{
        //    var adjustedImage = new System.Drawing.Bitmap(originalImage);
        //    float brightness = 1.0f; // no change in brightness
        //    float gamma = 1.0f; // no change in gamma

        //    float adjustedBrightness = brightness - 1.0f;
        //    // create matrix that will brighten and contrast the image
        //    float[][] ptsArray ={
        //new float[] {contrast, 0, 0, 0, 0}, // scale red
        //new float[] {0, contrast, 0, 0, 0}, // scale green
        //new float[] {0, 0, contrast, 0, 0}, // scale blue
        //new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
        //new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

        //    var imageAttributes = new ImageAttributes();
        //    imageAttributes.ClearColorMatrix();
        //    imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        //    imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
        //    System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(adjustedImage);
        //    g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height)
        //        , 0, 0, originalImage.Width, originalImage.Height,
        //        System.Drawing.GraphicsUnit.Pixel, imageAttributes);

        //    return adjustedImage;
        //}

        public void LoadImage(string path)
        {
            using (var img = System.Drawing.Image.FromFile(path))
            using (var mem = new MemoryStream())
            {
                img.Save(mem, ImageFormat.Bmp);
                var bmp = new System.Drawing.Bitmap(mem);
                originalBitmap = bmp;
                SetImage(bmp);
            }

            changeContrastBtn.IsEnabled = true;
        }


        // regular
        void WorkOnCalulatedFTTs()
        {



            /*
           * Code that works on the Calulated FFTs
           * */
            var differences = new List<double[]>();
            var differencesRight = new List<double[]>();
            var differencesTwoAdj = new List<double[]>();

            var phases = new List<double[]>();
            var mags = new List<double[]>();

            var maxMag = 0d;
            var minMag = double.MaxValue;
            var realMaxMag = 0d;
            var realMinMag = double.MaxValue;
            var sum = 0d;

            var minMagForIgnore = 0d;
            double.TryParse(minMagTxt.Text, out minMagForIgnore);


            var maxMagForIgnore = double.MaxValue;
            double.TryParse(maxMagTxt.Text, out maxMagForIgnore);
            // If max mag is 0 then nothing will show. In this case ignore the feature
            if (maxMagForIgnore == 0)
                maxMagForIgnore = double.MaxValue;


            var maxPhase = 0d;
            var maxDiff = 0d;
            var maxDiffRight = 0d;
            var maxDiffTwoAdj = 0d;

            for (int t = 0; t < AllFFT.Count; t++)
            {
                var curr = new double[AllFFT[t].Length];
                mags.Add(curr);

                var currPhases = new double[AllFFT[t].Length];
                phases.Add(currPhases);

                var currDiffs = new double[AllFFT[t].Length];
                differences.Add(currDiffs);

                var currDiffsRight = new double[AllFFT[t].Length];
                differencesRight.Add(currDiffsRight);

                var currDiffsTwoAdj = new double[AllFFT[t].Length];
                differencesTwoAdj.Add(currDiffsTwoAdj);

                for (int k = 0; k < AllFFT[t].Length; k++)
                {
                    var mag1 = Math.Sqrt(Math.Pow(AllFFT[t][k].X, 2) + Math.Pow(AllFFT[t][k].Y, 2)) / AllFFT[0].Length;
                    var realMag = mag1;






                    curr[k] = mag1;


                    var phase = (Math.Atan2(AllFFT[t][k].Y, AllFFT[t][k].X));
                    currPhases[k] = phase;

                    if (k == 0 || k == AllFFT[0].Length - 1 || t == 0)
                        currDiffs[k] = 0;
                    else
                        currDiffs[k] =
                            (Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t - 1][k - 1].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t - 1][k - 1].Y, 2)) +
                            Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t - 1][k + 1].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t - 1][k + 1].Y, 2))) / 2;

                    if (k == 0 || k == AllFFT[0].Length - 1 || t == AllFFT.Count - 1)
                        currDiffsRight[k] = 0;
                    else
                        currDiffsRight[k] =
                            (Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t + 1][k - 1].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t + 1][k - 1].Y, 2)) +
                            Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t + 1][k + 1].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t + 1][k + 1].Y, 2))) / 2;


                    if (k <= 1 || k >= AllFFT[0].Length - 2 || t <= 1 || t >= AllFFT.Count - 2)
                        currDiffsTwoAdj[k] = 0;
                    else
                        currDiffsTwoAdj[k] =
                            (Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t + 2][k - 2].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t + 2][k - 2].Y, 2)) +
                             Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t + 2][k + 2].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t + 2][k + 2].Y, 2)) +
                             Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t - 2][k - 2].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t - 2][k - 2].Y, 2)) +
                             Math.Sqrt(Math.Pow(AllFFT[t][k].X - AllFFT[t - 2][k + 2].X, 2) + Math.Pow(AllFFT[t][k].Y - AllFFT[t - 2][k + 2].Y, 2))
                            ) / 4;


                    if (curr[k] > maxMag)
                        maxMag = curr[k];

                    if (curr[k] < minMag && curr[k] != 0)
                        minMag = curr[k];

                    if (realMag > realMaxMag)
                        realMaxMag = realMag;

                    if (realMag < realMinMag)
                        realMinMag = realMag;

                    sum += curr[k];

                    if (phase > maxPhase)
                        maxPhase = phase;


                    if (currDiffs[k] > maxDiff)
                        maxDiff = currDiffs[k];

                    if (currDiffsRight[k] > maxDiffRight)
                        maxDiffRight = currDiffsRight[k];



                    if (currDiffsTwoAdj[k] > maxDiffTwoAdj)
                        maxDiffTwoAdj = currDiffsTwoAdj[k];
                }
            }

            minMagLbl.Text = realMinMag.ToString();
            maxMagLbl.Text = realMaxMag.ToString();

            var mean = sum / (AllFFT.Count * AllFFT[0].Length);

            float powerOfMagBlue = float.Parse(blueMagnitudePowerTxt.Text);
            var powerOfMagGreen = float.Parse(greenMagnitudePowerTxt.Text);
            var powerOfMagRed = float.Parse(redMagnitudePowerTxt.Text);

            //var powMagFactorBlue = 255 / (Math.Pow(maxMag, powerOfMagBlue));
            //var powMagFactorGreen = 255 / (Math.Pow(maxMag, powerOfMagGreen));
            //var powMagFactorRed = 255 / (Math.Pow(maxMag, powerOfMagRed));

            var powMagFactorBlue = 255 / (Math.Pow(maxMag - minMag, powerOfMagBlue));
            var powMagFactorGreen = 255 / (Math.Pow(maxMag - minMag, powerOfMagGreen));
            var powMagFactorRed = 255 / (Math.Pow(maxMag - minMag, powerOfMagRed));


            var rgbFactor = (255 / maxMag);
            var phaseFactor = (255d / maxPhase);
            var bFactor = (255 / maxDiff);
            var gFactor = (255 / maxDiffRight);
            var twoAdjFactor = (255 / maxDiffTwoAdj);

            var maxBlue = double.Parse(maxBlueTxt.Text);
            var maxGreen = double.Parse(maxGreenTxt.Text);
            var maxRed = double.Parse(maxRedTxt.Text);

            var minBlue = double.Parse(minBlueTxt.Text);
            var minGreen = double.Parse(minGreenTxt.Text);
            var minRed = double.Parse(minRedTxt.Text);

            var unitedColorsFactor = (255 * 3) / Math.Pow(maxMag - minMag, powerOfMagBlue);

            var bmp = new System.Drawing.Bitmap(AllFFT.Count, defaultfftLength);

            var minPresentedMag = double.MaxValue;
            var maxPresentedMag = double.MinValue;

            for (int t = 0; t < AllFFT.Count; t++)
            {
                for (int k = 0; k < AllFFT[t].Length; k++)
                {
                    var alpha = (byte)255;
                    byte powMagBlue = 0, powMagGreen = 0, powMagRed = 0;


                    if (magRangeChk.IsChecked.HasValue && magRangeChk.IsChecked.Value)
                    {
                        if (mags[t][k] < maxMagForIgnore && mags[t][k] > minMagForIgnore)
                        {
                            if (maxBlue > 0 && maxBlue >= minBlue)
                                powMagBlue = 255;
                            if (maxGreen > 0 && maxGreen >= minGreen)
                                powMagGreen = 255;
                            if (maxRed > 0 && maxRed >= minRed)
                                powMagRed = 255;

                        }
                    }
                    else
                    {
                        powMagBlue = (byte)Math.Round(Math.Pow(mags[t][k] - minMag, powerOfMagBlue) * powMagFactorBlue);
                        powMagGreen = (byte)Math.Round(Math.Pow(mags[t][k] - minMag, powerOfMagGreen) * powMagFactorGreen);
                        powMagRed = (byte)Math.Round(Math.Pow(mags[t][k] - minMag, powerOfMagGreen) * powMagFactorRed);
                        if (mags[t][k] == 0)
                        {
                            powMagBlue = 0;
                            powMagGreen = 0;
                            powMagRed = 0;
                        }



                        //if (unitedColors)
                        //{
                        //    var currHue = Math.Round(Math.Pow(mags[t][k] - minMag, powerOfMagBlue) * unitedColorsFactor);
                        //    if (currHue > 255)
                        //        powMagBlue = (byte)255;
                        //    else
                        //        powMagBlue = (byte)currHue;

                        //    if (currHue > 255 * 2)
                        //        powMagGreen = 255;
                        //    else if (currHue > 255)
                        //        powMagGreen = (byte)(currHue - 255);
                        //    else
                        //        powMagGreen = 0;

                        //    if (currHue > 255 * 2)
                        //        powMagRed = (byte)(currHue - 255 * 2);
                        //    else
                        //        powMagRed = (byte)0;


                        //}

                        if (reverseChk.IsChecked.HasValue && reverseChk.IsChecked.Value)
                        {
                            powMagBlue = powMagBlue == 0 ? (byte)255 : (byte)0;
                            powMagGreen = powMagGreen == 0 ? (byte)255 : (byte)0;
                            powMagRed = powMagRed == 0 ? (byte)255 : (byte)0;

                        }

                        if (powMagBlue > maxBlue)
                            powMagBlue = 0;
                        if (powMagGreen > maxGreen)
                            powMagGreen = 0;
                        if (powMagRed > maxRed)
                            powMagRed = 0;

                        if (powMagBlue < minBlue)
                            powMagBlue = 0;
                        if (powMagGreen < minGreen)
                            powMagGreen = 0;
                        if (powMagRed < minRed)
                            powMagRed = 0;

                        if (powMagBlue > 0)
                            powMagBlue = 255;
                        if (powMagGreen > 0)
                            powMagGreen = 255;
                        if (powMagRed > 0)
                            powMagRed = 255;
                    }

                    if (powMagBlue > 0 || powMagGreen > 0 || powMagRed > 0)
                    {
                        if (mags[t][k] > maxPresentedMag)
                            maxPresentedMag = mags[t][k];
                        if (mags[t][k] < minPresentedMag)
                            minPresentedMag = mags[t][k];
                    }


                    //var c = Color.FromArgb(alpha, r, g, b);
                    var c = Color.FromArgb(alpha, powMagRed, powMagGreen, powMagBlue);
                    bmp.SetPixel(
                        AllFFT.Count - t - 1,
                        bmp.Height - k - 1,
                        System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B));

                    //var rect = new Rectangle() { Width = width, Height = height };
                    //rect.SetValue(Canvas.LeftProperty, (AllFFT.Count - t) * width);
                    ////rect.SetValue(Canvas.BottomProperty, (AllFFT[0].Length- (k + 1d)) * height);
                    //rect.SetValue(Canvas.BottomProperty, (k + 1d) * height);

                    //rect.Fill = new SolidColorBrush(c);
                    //letter.Children.Add(rect);
                }
            }

            minPresentedMagLbl.Text = minPresentedMag.ToString();
            maxPresentedMagLbl.Text = maxPresentedMag.ToString();

            originalBitmap = bmp;

            //bmp = SetContrast(bmp);

            SetImage(bmp);
        }

        private void BmpToImage(System.Drawing.Bitmap bmp, Image img)
        {
            using (var memory = new MemoryStream())
            {
                bmp.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                img.Source = bitmapImage;
            }
        }

        public void SetImage(System.Drawing.Bitmap bmp)
        {

            BmpToImage(bmp, img);


            var size = float.Parse(bmpSizeTxt.Text);
            img.Width = size;
            img.Height = size;

        }


        System.Drawing.Bitmap originalBitmap;

        void player_FftCalculated(object sender, FftEventArgs e)
        {
            var fft = e.Result;
            var copy = fft.Select(x => x.Clone()).ToArray();
            AllFFT.Add(copy);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //contrastTxt.Text = contrast.ToString();
            filePathTxt.Text = path;

            try
            {
                if (File.Exists("settings.xml")) LoadSettings("settings.xml");


            }
            catch (Exception ex)
            {
                errLbl.Content = "An error occured while loading saved settings. The program continues. Error: " + ex.ToString();
            }
        }



        private void startBtn_Click(object sender, RoutedEventArgs e)
        {

            Start();
        }

        private bool ChangeContrast()
        {
            if (!Validate())
                return false;

            //var isFloat = float.TryParse(contrastTxt.Text, out contrast);
            //if (!isFloat)
            //{
            //    errLbl.Content = "Invalid float for contrast";
            //    return false;
            //}

            //var bmp = SetContrast(originalBitmap);
            //SetImage(bmp);
            SetImage(originalBitmap);


            return true;
        }

        private bool Start()
        {
            if (!Validate())
                return false;


            if (ready)
                lock (this)
                {
                    if (ready)
                        ready = false;
                    else
                        return false;
                }

            defaultfftLength = int.Parse((windowSizeCmb.SelectedItem as ComboBoxItem).Content.ToString());

            slider.Value = 1;

            //contrast = float.Parse(contrastTxt.Text);



            AllFFT = new List<Complex[]>();
            path = filePathTxt.Text;


            try
            {
                SaveSettings("settings.xml");
                //using (var w = new StreamWriter("settings.txt", false))
                //{
                //    w.WriteLine(path);
                //    w.WriteLine(contrast);
                //    w.WriteLine(bmpSizeTxt.Text);
                //    w.WriteLine(blueMagnitudePowerTxt.Text);
                //    w.WriteLine(greenMagnitudePowerTxt.Text);
                //    w.WriteLine(useRedChk.IsChecked.ToString());
                //    w.WriteLine(redMagnitudePowerTxt.Text);
                //    w.WriteLine((windowSizeCmb.SelectedItem as ComboBoxItem).Content);
                //}

            }
            catch (Exception ex)
            {
                errLbl.Content = "An error occured while saving settings. The program continues. Error: " + ex.ToString();
            }

            try
            {
                Run();
                return true;
            }
            catch (Exception ex)
            {
                errLbl.Content = "An error occured while running the program. The program stopped. Error: " + ex.ToString();
                startBtn.Background = new SolidColorBrush(Colors.Red);
                return false;
            }
        }

        private bool SaveSettings(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                errLbl.Content = "Export path cannot be empty";
                return false;
            }
            else if (path == "default.xml")
            {
                errLbl.Content = "Cannot override default.xml";
                return false;
            }
            else if (path.StartsWith("Tutorial"))
            {
                errLbl.Content = "Cannot override tutorial settings";
                return false;
            }
            else if (path.Contains("\\Samples\\"))
            {
                errLbl.Content = "Cannot override built in samples";
                return false;
            }

            var lastIdxOfSlash = path.LastIndexOf("\\");

            if (lastIdxOfSlash != -1)
            {
                var dirPath = path.Substring(0, lastIdxOfSlash);
                if (!Directory.Exists(dirPath))
                {
                    errLbl.Content = "Export directory path doesn't exist";
                    return false;
                }
            }


            try
            {
                var serializer = new DataContractSerializer(typeof(Settings));
                var settings = CreateSettings();
                using (var stream = File.Open(path, FileMode.Create))
                {
                    serializer.WriteObject(stream, settings);
                }
            }
            catch (Exception ex)
            {
                errLbl.Content = ex.ToString();
                return false;
            }

            return true;
        }

        public void Dispose()
        {
            if (player != null)
                player.Dispose();

        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Start();
            else if (
                e.Key == Key.Tab &&
                changeContrastBtn.IsEnabled)
                ChangeContrast();
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.D0)
                slider.Value = 1;
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.E)
                exportSettingsBtn_Click(null, null);
            else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.I)
                importSettingsBtn_Click(null, null);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            mouseLocationXTxt.Text = e.GetPosition(this).X.ToString();
            mouseLocationYTxt.Text = e.GetPosition(this).Y.ToString();
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                slider.Value += e.Delta / 1000d;
        }

        private void changeContrastBtn_Click(object sender, RoutedEventArgs e)
        {
            ChangeContrast();
        }

        private void loadImageBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadImage(imagePathTxt.Text);
        }

        private void exportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveSettings(settingsExportPath.Text))
                return;


            errLbl.Content = "";

            MessageBox.Show("Exported");
        }

        private void importSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!LoadSettings(settingsImportPath.Text, true))
                return;


            errLbl.Content = "";
            MessageBox.Show("Imported");
        }

        private bool LoadSettings(string path, bool ignorePathes = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                errLbl.Content = "Import path cannot be empty";
                return false;
            }

            if (!File.Exists(path))
            {
                errLbl.Content = "Import path file does not exist";
                return false;
            }

            try
            {
                var serializer = new DataContractSerializer(typeof(Settings));
                using (var stream = File.Open(path, FileMode.OpenOrCreate))
                {
                    var settings = (Settings)serializer.ReadObject(stream);
                    FromSettings(settings, ignorePathes);
                }

            }
            catch (Exception ex)
            {
                errLbl.Content = ex.ToString();
                return false;
            }
            return true;
        }

        private void defaultBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings("default.xml", true);
        }

        private void startTutorialBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists("Tutorial"))
            {
                errLbl.Content = "The tutorial directory doesn't exist. Please copy it from the original installation file.";
                return;
            }

            lastTutorialStep =
                new DirectoryInfo("Tutorial").GetFiles()
                .Where(f => f.Name.StartsWith("Step") || f.Name.StartsWith("step"))
                .Select(f =>
                {
                    var sub = f.Name.Substring("step".Length, f.Name.IndexOf(".") - "step".Length);
                    int x;
                    if (int.TryParse(sub, out x))
                        return x;
                    return -1;
                })
                .Max();

            tutorialLbl.Visibility = System.Windows.Visibility.Visible;
            tutorialMode = true;
            startTutorialBtn.IsEnabled = false;
            nextTutorialStepBtn.IsEnabled = true;
            pauseTutorialBtn.IsEnabled = true;
            startBtn.IsEnabled = false;
            changeContrastBtn.IsEnabled = false;
            sampleBtn.IsEnabled = false;
            TutorialStep();



        }

        private void TutorialStep()
        {
            string comment;
            var ok = ReadCurrentTutorialComment(out comment);
            if (!ok)
                return;
            tutorialLbl.Text = comment;

            string[] commands;
            ok = ReadCurrentTutorialCommands(out commands);
            if (!ok)
                return;

            ok = ExecuteCommands(commands);
            if (!ok)
                return;
        }

        private bool ExecuteCommands(string[] commands)
        {
            AllBlack();

            try
            {
                foreach (var cmd in commands)
                {
                    // Means it's a set value command
                    if (cmd.Contains(','))
                    {
                        var splitted = cmd.Split(',').Select(x => x.Trim()).ToArray();
                        var switchOver = splitted[0];
                        var justPainting = false;
                        if (splitted[0] == "!paint")
                        {
                            justPainting = true;
                            switchOver = splitted[1];
                        }

                        switch (switchOver)
                        {
                            case "filePathTxt":
                                if (!justPainting)
                                    filePathTxt.Text = splitted[1];
                                Blue(filePathTxt);
                                break;
                            case "windowSizeCmb":
                                if (!justPainting)
                                    windowSizeCmb.SelectedItem = windowSizeCmb.Items.Cast<ComboBoxItem>().Where(item => item.Content.ToString() == splitted[1]).SingleOrDefault();
                                Blue(windowSizeCmb);
                                break;
                            case "blueMagnitudePowerTxt":
                                if (!justPainting)
                                    blueMagnitudePowerTxt.Text = splitted[1];
                                Blue(blueMagnitudePowerTxt);
                                break;
                            case "greenMagnitudePowerTxt":
                                if (!justPainting)
                                    greenMagnitudePowerTxt.Text = splitted[1];
                                Blue(greenMagnitudePowerTxt);
                                break;
                            case "redMagnitudePowerTxt":
                                if (!justPainting)
                                    redMagnitudePowerTxt.Text = splitted[1];
                                Blue(redMagnitudePowerTxt);
                                break;
                            case "maxBlueTxt":
                                if (!justPainting)
                                    maxBlueTxt.Text = splitted[1];
                                Blue(maxBlueTxt);
                                break;
                            case "maxGreenTxt":
                                if (!justPainting)
                                    maxGreenTxt.Text = splitted[1];
                                Blue(maxGreenTxt);
                                break;
                            case "maxRedTxt":
                                if (!justPainting)
                                    maxRedTxt.Text = splitted[1];
                                Blue(maxRedTxt);
                                break;
                            case "minBlueTxt":
                                if (!justPainting)
                                    minBlueTxt.Text = splitted[1];
                                Blue(minBlueTxt);
                                break;
                            case "minGreenTxt":
                                if (!justPainting)
                                    minGreenTxt.Text = splitted[1];
                                Blue(minGreenTxt);
                                break;
                            case "minRedTxt":
                                if (!justPainting)
                                    minRedTxt.Text = splitted[1];
                                Blue(minRedTxt);
                                break;
                            case "minMagTxt":
                                if (!justPainting)
                                    minMagTxt.Text = splitted[1];
                                Blue(minMagTxt);
                                break;
                            case "maxMagTxt":
                                if (!justPainting)
                                    maxMagTxt.Text = splitted[1];
                                Blue(maxMagTxt);
                                break;
                            case "magRangeChk":
                                magRangeChk.IsChecked = bool.Parse(splitted[1]);
                                break;
                            //case "contrastTxt":
                            //    contrastTxt.Text = splitted[1];
                            //    Blue(contrastTxt);
                            //    break;
                            case "bmpSizeTxt":
                                if (!justPainting)
                                    bmpSizeTxt.Text = splitted[1];
                                Blue(bmpSizeTxt);
                                break;
                            case "settingsImportPath":
                                if (!justPainting)
                                    settingsImportPath.Text = splitted[1];
                                Blue(settingsImportPath);
                                break;
                            case "settingsExportPath":
                                if (!justPainting)
                                    settingsExportPath.Text = splitted[1];
                                Blue(settingsExportPath);
                                break;
                            case "minMagLbl":
                                Blue(minMagLbl);
                                break;
                            case "maxMagLbl":
                                Blue(maxMagLbl);
                                break;
                            case "minPresentedMagLbl":
                                Blue(minPresentedMagLbl);
                                break;
                            case "maxPresentedMagLbl":
                                Blue(maxPresentedMagLbl);
                                break;

                        }

                    }
                    else
                    {
                        bool ok;
                        switch (cmd)
                        {
                            case "!import":
                                ok = LoadSettings(settingsImportPath.Text, true);
                                if (!ok)
                                {
                                    pauseTutorialBtn_Click(null, null);
                                    commands = null;
                                    return false;
                                }
                                break;
                            case "!run":
                                ok = Start();
                                if (!ok)
                                {
                                    pauseTutorialBtn_Click(null, null);
                                    commands = null;
                                    return false;
                                }
                                break;
                            case "!size":
                                ok = ChangeContrast();
                                if (!ok)
                                {
                                    pauseTutorialBtn_Click(null, null);
                                    commands = null;
                                    return false;
                                }
                                break;
                            default:
                                throw new Exception("Illigal command in tutorial step " + tutorialStep + ". cmd=" + cmd ?? "null");

                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errLbl.Content = "Error while reading tutorial commands for step " + tutorialStep + "." + Environment.NewLine + ex.ToString();
                pauseTutorialBtn_Click(null, null);
                commands = null;
                return false;
            }
        }

        private bool ReadCurrentTutorialCommands(out string[] commands)
        {
            try
            {
                using (var r = new StreamReader("Tutorial\\Step" + tutorialStep + ".commands"))
                {
                    var cmd = new List<string>();
                    while (!r.EndOfStream)
                        cmd.Add(r.ReadLine());

                    commands = cmd.ToArray();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errLbl.Content = "Error while reading tutorial commands for step " + tutorialStep + "." + Environment.NewLine + ex.ToString();
                pauseTutorialBtn_Click(null, null);
                commands = null;
                return false;
            }
        }

        private bool ReadCurrentTutorialComment(out string comment)
        {
            try
            {
                using (var r = new StreamReader("Tutorial\\Step" + tutorialStep + ".txt"))
                {
                    comment = r.ReadToEnd();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errLbl.Content = "Error while reading tutorial comment for step " + tutorialStep + "." + Environment.NewLine + ex.ToString();
                pauseTutorialBtn_Click(null, null);
                comment = null;
                return false;
            }
        }

        private void pauseTutorialBtn_Click(object sender, RoutedEventArgs e)
        {
            tutorialMode = false;
            startTutorialBtn.IsEnabled = true;
            startBtn.IsEnabled = true;
            nextTutorialStepBtn.IsEnabled = false;
            pauseTutorialBtn.IsEnabled = false;
            changeContrastBtn.IsEnabled = true;
            sampleBtn.IsEnabled = true;
            tutorialLbl.Text = "";
            tutorialLbl.Visibility = System.Windows.Visibility.Collapsed;
            defaultBtn_Click(null, null);
        }

        private void restartTutorial_Click(object sender, RoutedEventArgs e)
        {
            tutorialStep = 1;
            startTutorialBtn_Click(sender, e);
        }

        private void nextTutorialStepBtn_Click(object sender, RoutedEventArgs e)
        {
            if (tutorialStep == lastTutorialStep)
            {
                MessageBox.Show("The tutorial is finished. Notice that the pathes of the wav file and import settings stayed as they were at the end of the tutorial, and the rest of the settings were restored to their default values.");
                pauseTutorialBtn_Click(null, null);
                tutorialStep = 1;
                return;
            }

            tutorialStep++;
            TutorialStep();
        }

        private void samplesDirsCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (samplesDirsCmb.SelectedItem != null)
                samplesSubDirsCmb.SelectedItem = ((SamplesDir)samplesDirsCmb.SelectedItem).SubDirs.FirstOrDefault();
        }

        private void samplesSubDirsCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (samplesSubDirsCmb.SelectedItem != null)
                samplesCmb.SelectedItem = ((SamplesDir)samplesSubDirsCmb.SelectedItem).Files.FirstOrDefault();

        }

        private void sampleBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedSample = samplesCmb.SelectedItem as Sample;
            if (selectedSample == null)
                return;

            settingsImportPath.Text = selectedSample.SettingsPath;
            filePathTxt.Text = selectedSample.WavPath;

            var ok = LoadSettings(settingsImportPath.Text, true);
            if (ok)
                Start();

            if (ImgWin != null)
                ImgWin.Close();

            ImgWin = new ImagePresenter(selectedSample.ImagePath);
            ImgWin.Topmost = true;
            ImgWin.Show();
        }

        private void samplesCmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedSample = samplesCmb.SelectedItem as Sample;
            if (selectedSample == null)
                return;
            sampleImg.Source = new BitmapImage(new Uri(selectedSample.ImagePath, UriKind.Absolute));

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ImgWin != null)
                ImgWin.Close();
        }

        private void currentPathBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(new DirectoryInfo(".\\").FullName);
        }





    }
}
