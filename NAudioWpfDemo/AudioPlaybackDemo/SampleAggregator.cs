using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace NAudioWpfDemo
{
    public class SampleAggregator : ISampleProvider
    {
        // volume
        public event EventHandler<MaxSampleEventArgs> MaximumCalculated;
        private float maxValue;
        private float minValue;
        public int NotificationCount { get; set; }
        int count;

        // FFT
        public event EventHandler<FftEventArgs> FftCalculated;
        public bool PerformFFT { get; set; }
        private Complex[] fftBuffer;
        private readonly FftEventArgs fftArgs;
        private int fftPos;
        private readonly int fftLength;
        private int m;
        private readonly ISampleProvider source;

        private readonly int channels;
        public int window ;
        public static int defaultfftLength = 1024;

        //public SampleAggregator(ISampleProvider source, int fftLength = 1024) // ain,kaf
        //public SampleAggregator(ISampleProvider source, int fftLength = 2048)
        public SampleAggregator(ISampleProvider source, int fftLength = 0) // good for kuf
        {
            if (fftLength == 0)
                fftLength = defaultfftLength;

            window = fftLength;
            channels = source.WaveFormat.Channels;
            if (!IsPowerOfTwo(fftLength))
            {
                throw new ArgumentException("FFT Length must be a power of two");
            }
            this.m = (int)Math.Log(fftLength, 2.0);
            this.fftLength = fftLength;
            this.fftBuffer = new Complex[fftLength];
            //this.fftBuffer = new Complex[fftLength + window]; // once with once without
            //this.fftBuffer = new Complex[fftLength * 2 - window]; // use it two times
            this.fftArgs = new FftEventArgs(fftBuffer);
            this.source = source;
        }

        bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }


        public void Reset()
        {
            count = 0;
            maxValue = minValue = 0;
        }

        public static List<Complex[]> FFTDataCalculated = new List<Complex[]>();

        public static List<float> raw = new List<float>();




        public static List<float[]> FFts = new List<float[]>();

        private void Add(float value)
        {
            if (PerformFFT && FftCalculated != null)
            {


                fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
                //fftBuffer[fftPos].X = (float)(value * FastFourierTransform.BlackmannHarrisWindow(fftPos, fftLength));
                //fftBuffer[fftPos].X = value;
                fftBuffer[fftPos].Y = 0;
                fftPos++;
                if (fftPos >= fftBuffer.Length)
                {
                    fftPos = 0;
                    //var copyBuffer = new Complex[fftBuffer.Length];
                    //fftPos = fftBuffer.Length / 2;
                    //for (int i = 0; i < fftBuffer.Length / 2; i++)
                    //{
                    //    copyBuffer[i] = fftBuffer[fftBuffer.Length / 2 + i].Clone();
                    //}



                    // nadav - copy ffts
                    var newFFT = new Complex[fftBuffer.Length];
                    for (int i = 0; i < fftBuffer.Length; i++)
                        newFFT[i] = fftBuffer[i].Clone();
                    FFTDataCalculated.Add(newFFT);

                    // 1024 = 2^10



                    FastFourierTransform.FFT(false, m, fftBuffer);

                    FftCalculated(this, new FftEventArgs(fftBuffer));

                    //fftBuffer = copyBuffer;

                    if (fftBuffer.Any(c => c.X != 0 || c.Y != 0))
                    {
                        var firstNotNul = fftBuffer.FirstOrDefault(c => c.X != 0 || c.Y != 0);
                        var x = firstNotNul.X;
                        var y = firstNotNul.Y;
                    }

                }
            }

            maxValue = Math.Max(maxValue, value);
            minValue = Math.Min(minValue, value);
            count++;
            if (count >= NotificationCount && NotificationCount > 0)
            {
                if (MaximumCalculated != null)
                {
                    MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue));
                }
                Reset();
            }
        }

        //public int window = 256; //shin
        //public int window = 2; //shin

        /// <summary>
        /// Once with window once without. do distance in mag
        /// </summary>
        /// <param name="value"></param>
        //private void Add(float value)
        //{
        //    if (PerformFFT && FftCalculated != null)
        //    {


        //        fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
        //        //fftBuffer[fftPos].X = value;


        //        fftBuffer[fftPos].Y = 0;
        //        fftPos++;
        //        if (fftPos >= fftBuffer.Length)
        //        {
        //            //fftPos = 0;


        //            //var copyBuffer = new Complex[fftBuffer.Length];
        //            //fftPos = 3 * fftBuffer.Length / 4;
        //            //for (int i = 0; i < 3 * fftBuffer.Length / 4; i++)
        //            //{
        //            //    copyBuffer[i] = fftBuffer[fftBuffer.Length / 4 + i].Clone();
        //            //}
        //            var copyBuffer = new Complex[fftBuffer.Length];
        //            fftPos = fftBuffer.Length / 2;
        //            for (int i = 0; i < fftBuffer.Length / 2; i++)
        //            {
        //                copyBuffer[i] = fftBuffer[fftBuffer.Length / 2 + i].Clone();
        //            }
        //            //var copyBuffer = new Complex[fftBuffer.Length];
        //            //fftPos = 7 * fftBuffer.Length / 8;
        //            //for (int i = 0; i < 7 * fftBuffer.Length / 8; i++)
        //            //{
        //            //    copyBuffer[i] = fftBuffer[fftBuffer.Length / 8 + i].Clone();
        //            //}

        //            // nadav - copy ffts
        //            var newFFT = new Complex[fftBuffer.Length];
        //            for (int i = 0; i < fftBuffer.Length; i++)
        //                newFFT[i] = fftBuffer[i].Clone();
        //            FFTDataCalculated.Add(newFFT);

        //            // 1024 = 2^10


        //            var fft1 = new Complex[fftBuffer.Length - window];
        //            var fft2 = new Complex[fftBuffer.Length - window];
        //            for (int i = 0; i < fftBuffer.Length - window; i++)
        //            {
        //                fft1[i] = fftBuffer[i].Clone();
        //                fft2[i] = fftBuffer[i + window].Clone();
        //            }

        //            FastFourierTransform.FFT(false, m, fft1);
        //            FastFourierTransform.FFT(false, m, fft2);


        //            FftCalculated(this, new FftEventArgs(fft1) { Result2 = fft2 });




        //            fftBuffer = copyBuffer;

        //            if (fftBuffer.Any(c => c.X != 0 || c.Y != 0))
        //            {
        //                var firstNotNul = fftBuffer.FirstOrDefault(c => c.X != 0 || c.Y != 0);
        //                var x = firstNotNul.X;
        //                var y = firstNotNul.Y;
        //            }

        //        }
        //    }

        //    maxValue = Math.Max(maxValue, value);
        //    minValue = Math.Min(minValue, value);
        //    count++;
        //    if (count >= NotificationCount && NotificationCount > 0)
        //    {
        //        if (MaximumCalculated != null)
        //        {
        //            MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue));
        //        }
        //        Reset();
        //    }
        //}

        /// <summary>
        /// Window in the middle - use it twice
        /// </summary>
        /// <param name="value"></param>
        //private void Add(float value)
        //{
        //    if (PerformFFT && FftCalculated != null)
        //    {


        //        //fftBuffer[fftPos].X = (float)(value * FastFourierTransform.HammingWindow(fftPos, fftLength));
        //        fftBuffer[fftPos].X = value;
        //        fftBuffer[fftPos].Y = 0;
        //        fftPos++;
        //        if (fftPos >= fftBuffer.Length)
        //        {
        //            //fftPos = 0;
        //            //var copyBuffer = new Complex[fftBuffer.Length];
        //            //fftPos = 3*fftBuffer.Length / 4;
        //            //for (int i = 0; i < 3*fftBuffer.Length / 4; i++)
        //            //{
        //            //    copyBuffer[i] = fftBuffer[fftBuffer.Length / 4 + i].Clone();
        //            //}
        //            var copyBuffer = new Complex[fftBuffer.Length];
        //            fftPos = fftBuffer.Length / 2;
        //            for (int i = 0; i < fftBuffer.Length / 2; i++)
        //            {
        //                copyBuffer[i] = fftBuffer[fftBuffer.Length / 2 + i].Clone();
        //            }


        //            // nadav - copy ffts
        //            var newFFT = new Complex[fftBuffer.Length];
        //            for (int i = 0; i < fftBuffer.Length; i++)
        //                newFFT[i] = fftBuffer[i].Clone();
        //            FFTDataCalculated.Add(newFFT);

        //            // 1024 = 2^10


        //            var fft1 = new Complex[fftBuffer.Length - window];
        //            var fft2 = new Complex[fftBuffer.Length - window];

        //            // 100 length => 180 buffer. window starts at 80-100
        //            var winStartPos = (fftBuffer.Length) / 2 - window/2;

        //            for (int i = 0,j=winStartPos; i < fftLength; i++,j++)
        //            {
        //                fft1[i] = fftBuffer[i].Clone();
        //                fft2[i] = fftBuffer[j].Clone();
        //            }

        //            FastFourierTransform.FFT(false, m, fft1);
        //            FastFourierTransform.FFT(false, m, fft2);


        //            FftCalculated(this, new FftEventArgs(fft1) { Result2 = fft2 });




        //            fftBuffer = copyBuffer;

        //            if (fftBuffer.Any(c => c.X != 0 || c.Y != 0))
        //            {
        //                var firstNotNul = fftBuffer.FirstOrDefault(c => c.X != 0 || c.Y != 0);
        //                var x = firstNotNul.X;
        //                var y = firstNotNul.Y;
        //            }

        //        }
        //    }

        //    maxValue = Math.Max(maxValue, value);
        //    minValue = Math.Min(minValue, value);
        //    count++;
        //    if (count >= NotificationCount && NotificationCount > 0)
        //    {
        //        if (MaximumCalculated != null)
        //        {
        //            MaximumCalculated(this, new MaxSampleEventArgs(minValue, maxValue));
        //        }
        //        Reset();
        //    }
        //}

        public WaveFormat WaveFormat { get { return source.WaveFormat; } }

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);


            for (int n = 0; n < samplesRead; n += channels)
            {
                Add(buffer[n + offset]);
            }
            return samplesRead;
        }
    }

    public class MaxSampleEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public MaxSampleEventArgs(float minValue, float maxValue)
        {
            this.MaxSample = maxValue;
            this.MinSample = minValue;
        }
        public float MaxSample { get; private set; }
        public float MinSample { get; private set; }
    }

    public class FftEventArgs : EventArgs
    {
        [DebuggerStepThrough]
        public FftEventArgs(Complex[] result)
        {
            this.Result = result;
        }
        public Complex[] Result { get; private set; }
        public Complex[] Result2 { get; set; }
    }
}
