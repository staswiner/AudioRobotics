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
			//	NXT.Open("COM5");

		}
		~Form1()
		{
			//	NXT.close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			List<NAudio.Wave.WaveInCapabilities> sources = new List<NAudio.Wave.WaveInCapabilities>();
			for (int i = 0; i < NAudio.Wave.WaveIn.DeviceCount; i++)
			{
				sources.Add(NAudio.Wave.WaveIn.GetCapabilities(i));
			}

			listBox1.Items.Clear();
			foreach (var source in sources)
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
		public List<Int16> SingleWave = null; // 1 cycle
		public List<Int16> FrameWave = null; // 100ms
		public List<Int16> DifferentialsWave = null; // Differentials
		public List<Int16> FrequencySinus = null;
		public Dictionary<int,Int16> ExtremumsWave = null; // Extremums
		int VolumeThreshHold = 4000;
		private Int16 GetSampleValue(byte[] buffer, int index)
		{
			byte sound1 = buffer[index + 0];
			byte sound2 = buffer[index + 1];
			byte[] sound = { sound1, sound2 };
			Int16 value = BitConverter.ToInt16(sound, 0);
			return value;
		}
		private List<Int16> ConvertFormatData(byte[] Data, int Count)
		{
			List<Int16> Amplitudes = new List<Int16>();
			for (var i = 0; i < Count - 5; i += 4)
			{
				Int16 value1 = GetSampleValue(Data, i);
				Int16 value2 = GetSampleValue(Data, i+2);
				Amplitudes.Add(value2);
				Amplitudes.Add(value1);
			}
			return Amplitudes;
		}
		private int SFT(List<Int16> Amplitudes) // stas fürer transform
		{
			int StartTime = DateTime.Now.Millisecond;
			const int FullCaptureFraction = 256;
			int MirrorSize = ((Amplitudes.Count) / FullCaptureFraction) * 10;
			
			// Finds First Positive faggot after reaching 0
			int StartIndex = 0;
			for (var i = 3; i < Amplitudes.Count; i++)
			{
				if (Amplitudes[i-3] <= 0 && Amplitudes[i] > 0)
				{
					StartIndex = i;
					break;
				}
			}
			if (StartIndex > MirrorSize)
				return 0;

			FrameWave = new List<short>(Amplitudes);
			// <Key: Frequency, Value : Amplitude>
			Dictionary<int,int> FrequencyContainer = new Dictionary<int, int>();
			//int Frequency = FrequencySinusValue;

			for(int Frequency = 440; Frequency > 260; Frequency-=10)
			//int Frequency = 440;
			{
				FrequencyContainer[Frequency] = 0;
				bool Checked = true;
				const int AmplituteIncrement = 100;
				while (Checked)
				{
					// *32 = 32 times more steps
					for (float i = 0; i < ((float)MirrorSize / (float)Frequency) / 10; i+=1.0f/(Frequency*10.0f))
					{
						float SinusFunctionValue = 
							(System.Math.Abs(FrequencyContainer[Frequency]))
							* (float)System.Math.Sin((double)(i));

						if (System.Math.Abs(Amplitudes[(int)(i * (Frequency)) + StartIndex]) < 
							System.Math.Abs(SinusFunctionValue))
						{
							Checked = false;

							Int16[] myArray = new short[MirrorSize + StartIndex];
							for (int s = 0; s < StartIndex; s++)
							{
								myArray[s] = 0;
							}


							for (float s = 0; s < ((float)MirrorSize / (float)Frequency);
								s += 1.0f / (Frequency * 10.0f))
							{
								SinusFunctionValue =
									(System.Math.Abs(FrequencyContainer[Frequency]))
									* (float)System.Math.Sin((double)(s));

								myArray[(int)(s * (Frequency / 10.0f)) + StartIndex] =
									(Int16)SinusFunctionValue;
							}
							FrequencySinus = new List<Int16>(myArray);
							break;
						}

					}
					if (Checked == true)
						FrequencyContainer[Frequency] += AmplituteIncrement;
				}
			}




			


			int max = 0;
			int ReturnFrequency = 0;
			foreach(var i in FrequencyContainer)
			{
				if (i.Value > max)
				{
					max = i.Value;
					ReturnFrequency = i.Key;
				}
			}
			int EndTime = DateTime.Now.Millisecond;
			int TotalTime = EndTime - StartTime;
			return ReturnFrequency;
		}
		private int DistanceBetweenExtremums(List<Int16> Amplitudes)
		{
			Int16 MaxVal1 = 0;
			Int16 MinVal1 = 0;
			int maxValIndex = 0;
			int minValIndex = 0;
			int State = 0;
			// Goal, find max val
			for(int i = 0; i < Amplitudes.Count-10; i++)
			{
				if (Amplitudes[i] < -200 && State==0)
				{
					State = 1;
				}
				if (Amplitudes[i] > 200 && State==1)
				{
					State = 2; 
				}
				if (Amplitudes[i] < -200 && State==2)
				{
					break;
				}
				if (Amplitudes[i] > MaxVal1)
				{
					MaxVal1 = Amplitudes[i];
					maxValIndex = i;
				}
				if (Amplitudes[i] < MinVal1)
				{
					MinVal1 = Amplitudes[i];
					minValIndex = i;
				}
			}

			int Frequency = Amplitudes.Count / (System.Math.Abs(maxValIndex - minValIndex) * 2) * 10;
			return Frequency;
		}
		private void ProceedData(byte[] Data, int Count)
		{
			// <value of Amplitude>
			List<Int16> Amplitudes = new List<Int16>();
			// <value of differential>
			List<Int16> Differentials = new List<Int16>();
			// <index, Extremum (amplitute)>
			var Extremums = new Dictionary<int, Int16>();
			// Sum
			int IntegralSum = 0;

			for (var i = 0; i < Count - 200; i += 2)
			{
				Int16 value1 = GetSampleValue(Data, i);
				Int16 value2 = GetSampleValue(Data, i + 2);
				Int16 value3Far = GetSampleValue(Data, i + 20);
				Differentials.Add((short)(value3Far - value1));
				Amplitudes.Add(value1);
				IntegralSum += value1;
			}
			textBox2.Text = IntegralSum.ToString();
			FrameWave = new List<short>(Amplitudes);
			for (var i = 0; i < Count/2 - 200; i++)
			{
				if (Differentials[i] * Differentials[i + 1] < 0) // changed from neg to pos or pos to neg
				{
					Extremums[i] = Amplitudes[i];
				}
			}
			//
			int MatchIndex = 0;
			int MaxMatch = 1;
			Dictionary<int, Int16> CurrentContainer = new Dictionary<int, Int16>();
			Dictionary<int, Int16> LastContainer = new Dictionary<int, Int16>();
		
			for (int n = 0; Extremums.Count > 1; n++)
			{
				CurrentContainer.Clear();
				MatchIndex = 0;
				MaxMatch = 1;
				CurrentContainer[Extremums.ElementAt(0).Key] = Extremums.ElementAt(0).Value;
				for (int i = 1; i < Extremums.Count; i++)
				{
					if (Math.IsEqual(
						Extremums.ElementAt(i).Value,
						Extremums.ElementAt(MatchIndex).Value).To_Extent(50.0f) &&
						i > MatchIndex + 2)
					{
						if (MatchIndex == MaxMatch)
						{
							CurrentContainer[Extremums.ElementAt(i).Key] = Extremums.ElementAt(i).Value;
							MaxMatch++;
						}
						MatchIndex++;
					}
					else
					{
						MatchIndex = 0;
					}
				}
				LastContainer = new Dictionary<int, short>(Extremums);
				Extremums = new Dictionary<int, short>(CurrentContainer);
			}
			/**/

			SingleWave = new List<short>();
			for(int i = 0; i < LastContainer.ElementAt(LastContainer.Count - 1).Key; i++)
			{
				SingleWave.Add(Amplitudes[i]);
			}

			DifferentialsWave = new List<short>(Differentials);
			ExtremumsWave = new Dictionary<int, short>(Extremums);
		
		}
		private void sourceStream_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
		{
			int sample = e.BytesRecorded;
			/* analyze data */
			float totalData = 0;
			float max = 0;
			float InPositive = 0;
			bool WentNegative = true;
			bool isOdd = true;
			CurrentBuffer = new byte[sample];
			// for drawing
			for (int i = 0; i < sample - 3; i += 2)
			{
				Int16 value = GetSampleValue(e.Buffer, i);
				if (value > max) max = value;
			}
			VolumeThreshHold = (int)(max * 0.1f);
			VolumeThreshHold = 200;
			for (int i = 0; i < sample - 3; i += 2)
			{
				Int16 value = GetSampleValue(e.Buffer, i);
				if (isOdd)
				{
					value *= -1;
					isOdd = false;
				}
				else
				{
					isOdd = true;
				}


				if (value > VolumeThreshHold && WentNegative)
				{
					InPositive++;
					WentNegative = false;
				}
				if (value < -VolumeThreshHold && !!WentNegative == false)
				{
					WentNegative = true;
				}
				byte[] newValues = BitConverter.GetBytes(value);
				CurrentBuffer[i + 0] = newValues[0];
				CurrentBuffer[i + 1] = newValues[1];

			}
			//ProceedData(CurrentBuffer, sample);
			int ReturnFrequency = SFT(this.ConvertFormatData(CurrentBuffer, sample));
			//ReturnFrequency = this.DistanceBetweenExtremums(this.ConvertFormatData(CurrentBuffer, sample));
			textBox2.Text = ReturnFrequency.ToString();
			this.pictureBox1.Invalidate();
			//this.pictureBox2.Invalidate();
			//this.pictureBox3.Invalidate();
			//this.pictureBox4.Invalidate();

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
		private void Replay(object sender, StoppedEventArgs e)
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
			Brush brushPink = new SolidBrush(Color.DarkMagenta);
			int x = 0;
			int y = 0;
			float ScaleFactor = 1.0f / 10.0f;
			if (FrameWave != null)
			{
				Point currentDot;
				Point previousDot;
				e.Graphics.Clear(Color.White);
				previousDot = new Point();
				Int16 value = FrameWave[0];
				previousDot.Y = (int)((float)value * ScaleFactor + 100.0f);
				previousDot.X = 0;
				for (int i = 1; i < FrameWave.Count; i++)
				{

					value = FrameWave[i];
					int xDelta = i;

					e.Graphics.FillRectangle(brushRed, x + xDelta, 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + xDelta, VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + xDelta, -VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brush, x + xDelta, (float)value * ScaleFactor + 100.0f, 1, 1);
				}
			}
			if (FrequencySinus != null)
			{
				ScaleFactor = 1.0f / 10.0f;
				Point currentDot;
				Point previousDot;
				previousDot = new Point();
				float value = FrequencySinus[0];
				previousDot.Y = (int)((float)value * ScaleFactor + 100.0f);
				previousDot.X = 0;
				for (int i = 1; i < FrequencySinus.Count; i++)
				{

					value = FrequencySinus[i];
					int xDelta = i;

					//  e.Graphics.DrawLine(pen, currentDot, previousDot);
				
					e.Graphics.FillRectangle(brushPink, x + xDelta, (float)value * ScaleFactor + 100.0f, 2, 2);
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

		private void pictureBox2_Click(object sender, EventArgs e)
		{
			

		}

		private void pictureBox2_Paint(object sender, PaintEventArgs e)
		{
			Brush brush = new SolidBrush(Color.Black);
			Brush brushRed = new SolidBrush(Color.Red);
			Brush brushBlue = new SolidBrush(Color.Blue);
			int x = 0;
			int y = 0;
			float ScaleFactor = 1.0f / 1.0f;

			Point currentDot;
			Point previousDot;
			e.Graphics.Clear(Color.White);
			previousDot = new Point();
			if (DifferentialsWave != null)
			{
				Int16 value = DifferentialsWave[0];
	
				for (int i = 0; i < DifferentialsWave.Count; i++)
				{
					value = DifferentialsWave[i];

					e.Graphics.FillRectangle(brushRed, x + (i), 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i), VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i), -VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushRed, x + (i), value * ScaleFactor + 100.0f, 1, 1);

				}
			}
		}

		private void pictureBox3_Paint(object sender, PaintEventArgs e)
		{
			Brush brush = new SolidBrush(Color.Black);
			Brush brushRed = new SolidBrush(Color.Red);
			Brush brushBlue = new SolidBrush(Color.Blue);
			int x = 0;
			int y = 0;
			float ScaleFactor = 1.0f / 10.0f;

			Point currentDot;
			Point previousDot;
			e.Graphics.Clear(Color.White);
			previousDot = new Point();
			if (ExtremumsWave != null)
			{
	
				foreach (var i in ExtremumsWave)
				{
					Int16 value = i.Value;

					e.Graphics.FillRectangle(brushRed, x + (i.Key), 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i.Key), VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushBlue, x + (i.Key), -VolumeThreshHold * ScaleFactor + 100.0f, 1, 1);
					e.Graphics.FillRectangle(brushRed, x + (i.Key), value * ScaleFactor + 100.0f, 1, 1);

				}
			}
		}

		private void pictureBox4_Paint(object sender, PaintEventArgs e)
		{

		}
		private int FrequencySinusValue = 440;
		private void button6_Click(object sender, EventArgs e)
		{
			FrequencySinusValue--;
			pictureBox1.Invalidate();
		}
	}
}
