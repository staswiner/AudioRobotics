using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

namespace winformsAudio
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
			NXT.Open("COM5");

		}
		~Form1()
		{
			NXT.close();
		}

        private void button1_Click(object sender, EventArgs e)
        {
            List<NAudio.Wave.WaveInCapabilities> sources = new List<NAudio.Wave.WaveInCapabilities>();
            for(int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
            {
                sources.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
            }

            listBox1.Items.Clear();
            foreach( var source in sources)
            {
                ListViewItem item = new ListViewItem(source.ProductName);
                this.listBox1.Items.Add(item);
            }
        }

        private NAudio.Wave.WaveIn sourceStream = null;
        private NAudio.Wave.DirectSoundOut waveOut = null;
        private NAudio.Wave.WaveFileWriter waveWriter = null;
        NAudio.Wave.WaveInProvider waveIn = null;
		WaveChannel32 wave = null;

		private void button2_Click(object sender, EventArgs e)
        {
			if (wave != null)
			{
				wave.Dispose();
				wave = null;
			}
			while (Last10.Count < 10)
				Last10.Add(0);
            int DeviceNumber = 0;
			sourceStream = new NAudio.Wave.WaveIn();
            sourceStream.DeviceNumber = DeviceNumber;
            sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(DeviceNumber).Channels);
			sourceStream.BufferMilliseconds = 100;

			WaveFormat test = sourceStream.WaveFormat;
			waveIn = new NAudio.Wave.WaveInProvider(sourceStream);

            waveOut = new NAudio.Wave.DirectSoundOut();
            waveOut.Init(waveIn);
            sourceStream.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(sourceStream_DataAvailable);
            waveWriter = new NAudio.Wave.WaveFileWriter("Violin.wav", sourceStream.WaveFormat);

            sourceStream.StartRecording();
            waveOut.Stop();
        }
		float Average = 0;
		List<float> Last10 = new List<float>(10);
		public byte[] CurrentBuffer = null;
		public byte[] CurrentFloatBuffer = null;
		int VolumeThreshHold = 4000;
		private Int16 GetSampleValue(byte[] buffer, int index)
		{
			byte sound1 = buffer[index + 0];
			byte sound2 = buffer[index + 1];
			byte[] sound = { sound1, sound2 };
			Int16 value = BitConverter.ToInt16(sound, 0);
			return value;
		}
        private void sourceStream_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
			int sample = e.BytesRecorded;
			/* analyze data */
			float totalData = 0;
			float max = 0;
			float InPositive = 0;
			bool WentNegative = true;
            CurrentBuffer = new byte[sample];
			// for drawing
            for (int i = 0; i < sample - 3; i += 2)
            {
                Int16 value = GetSampleValue(e.Buffer, i);
                if (value > max) max = value;
            }
            VolumeThreshHold = (int)(max * 0.1f);
            VolumeThreshHold = 200;
            for (int i = 0; i < sample - 3; i+=2)
			{
				Int16 value = GetSampleValue(e.Buffer,i);
                if (value == max)
                {
                    value = value;
                }
                double PowerFactor = 1.0f;
                double FilteredValue = (double)Math.Pow((double)value, PowerFactor);
                FilteredValue /= (double)Math.Pow((double)max, PowerFactor-1.0f);
                value = (Int16)FilteredValue;
                //if (value < 0)
                //{
                //    value *= -1;
                //}
                if (value > VolumeThreshHold && WentNegative)
				{
					InPositive++;
					WentNegative = false;
				}
				if (value < -VolumeThreshHold && !!WentNegative == false)
				{
					WentNegative = true;
				}
                if (value < 0)
                {
                    value = value;
                }
                byte[] newValues = BitConverter.GetBytes(value);
                CurrentBuffer[i + 0] = newValues[1];
                CurrentBuffer[i + 1] = newValues[0];
            }

           
            this.pictureBox1.Invalidate();

            //totalData /= sample;
            //totalData = 10000.0f / totalData;
            //	InPositive *= 10.0f; //100ms, duplicate data for speakers

            Average = 0;
			int Low = 0;


            InPositive *= 10;
			textBox1.Text = InPositive.ToString();
			//textBox1.Text = Average.ToString();



            waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            waveWriter.Flush();
			waveIn.Read(e.Buffer, 0, e.BytesRecorded);
			count++;
			if (count == 10)
			{
				int help = 0;
			}
        }
		int count = 0;
        private void button5_Click(object sender, EventArgs e)
        {
			if (sourceStream != null)
			{
				sourceStream.StopRecording();
				sourceStream.Dispose();
				sourceStream = null;
			}
			if (waveWriter != null)
			{
	            waveWriter.Dispose();
				sourceStream = null;
			}

			OpenFile("Violin.wav");

        }
		DirectSoundOut output = new DirectSoundOut(200);
		private void OpenFile(string path)
		{
			wave = new WaveChannel32(new WaveFileReader(path));
			SoundEffect soundEffect = new SoundEffect(wave);
			BlockAlignReductionStream stream = new BlockAlignReductionStream(soundEffect);

			
			output.Init(stream);
			output.Play();
			output.PlaybackStopped += new EventHandler<StoppedEventArgs>(Replay);

		}
		private void Replay(object sender,StoppedEventArgs e)
		{
			wave = new WaveChannel32(new WaveFileReader("Violin.wav"));
			SoundEffect soundEffect = new SoundEffect(wave);
			BlockAlignReductionStream stream = new BlockAlignReductionStream(soundEffect);

			output.Init(stream);
			output.Play();
		}
		private void button3_Click(object sender, EventArgs e)
        {
            if (waveOut != null)
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                sourceStream.Dispose();
                sourceStream = null;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

		private void pictureBox1_Click(object sender, EventArgs e)
		{

		}

		private void pictureBox1_Paint(object sender, PaintEventArgs e)
		{
			Pen pen = new Pen(Color.FromArgb(255, 0, 0, 0));
			Brush brush = new SolidBrush(Color.Black);
			Brush brushRed = new SolidBrush(Color.Red);
			Brush brushBlue = new SolidBrush(Color.Blue);
			int x = 0;
			int y = 0;
					float ScaleFactor = 1.0f / 100.0f;
			if (CurrentBuffer != null)
			{
                Point currentDot;
                Point previousDot;
				e.Graphics.Clear(Color.White);
                previousDot = new Point();
                Int16 value = (Int16)(GetSampleValue(CurrentBuffer, 0) * -1);
                previousDot.Y = (int)((float)value * ScaleFactor + 100.0f);
                previousDot.X = 0;
                for (int i = 1; i < CurrentBuffer.Length - 3; i += 2)
				{

					value = (Int16)(GetSampleValue(CurrentBuffer, i));
                    if (value > 0)
                        value = value;
                    value *= -1;
                    currentDot = new Point();
                    currentDot.Y = (int)((float)value * ScaleFactor + 100.0f);
                    currentDot.X = (i/2);

                  //  e.Graphics.DrawLine(pen, currentDot, previousDot);
                    previousDot = currentDot;
					e.Graphics.FillRectangle(brushRed, x + (i / 2), 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i / 2), VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i / 2), -VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brush, x + (i / 2), value * ScaleFactor + 100.0f, 1, 1);
				}
			}
			if (CurrentFloatBuffer != null)
			{
				e.Graphics.Clear(Color.White);
				for (int i = 0; i < CurrentFloatBuffer.Length - 3; i += 8)
				{
					byte sound1 = CurrentFloatBuffer[i + 0];
					byte sound2 = CurrentFloatBuffer[i + 1];
					byte sound3 = CurrentFloatBuffer[i + 4];
					byte sound4 = CurrentFloatBuffer[i + 5];
					byte[] sound = { sound1, sound2, sound3, sound4 };
					float value = BitConverter.ToSingle(sound, 0);


					e.Graphics.FillRectangle(brushRed, x + (i / 2), 100.0f, 1, 1);
					e.Graphics.FillRectangle(brush, x + (i / 2), value, 1, 1);
				}
			}
			}
		Bluetooth NXT = new Bluetooth();
		private void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			var key = e.KeyCode;
			if (key == Keys.Right)
			{
				NXT.SetMotor(0, 100, Bluetooth.Motor.eState.On);
			}
			if (key == Keys.Left)
			{
				NXT.SetMotor(0, -100, Bluetooth.Motor.eState.On);
			}
			if (key == Keys.Space)
			{
				NXT.SetMotor(0, 0, Bluetooth.Motor.eState.Off);
			}
		}

		private void Form1_KeyUp(object sender, KeyEventArgs e)
		{
			var key = e.KeyCode;
			if (key == Keys.Right || key == Keys.Left)
			{
				NXT.SetMotor(1, 0, Bluetooth.Motor.eState.On);
			}
		}

		private void Form1_KeyPress(object sender, KeyPressEventArgs e)
		{

		}
	}
}
