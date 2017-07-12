/*
 *	Algorithm 1 : Line 170
 *	Algorithm 2 : Line 261
 *	Algorithm 3 : Line 293
 *	Bluetooth Protocol : Blueooth.cs 
 *	NXT orderes based on audio results : Line 455
 *	Event function uppon receiving audio input : Line 412
 * 
 * 
 * 
 * 
 * 
 * 
 */








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
		}
		~Form1()
		{
			NXT.close();
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
		public List<float> SingleWave = null; // 1 cycle
		public List<float> FrameWave = null; // 100ms
		public List<float> DifferentialsWave = null; // Differentials
		public List<Int16> FrequencySinus = null;
		public Dictionary<int,float> ExtremumsWave = null; // Extremums
		int VolumeThreshHold = 4000;
		void StoreInDrawingBuffer(byte[] Amplitudes, int Count)
		{
			FrameWave = ConvertFormatData(Amplitudes, Count);
		}
		private Int16 GetSampleValue(byte[] buffer, int index)
		{
			byte sound1 = buffer[index + 0];
			byte sound2 = buffer[index + 1];
			byte[] sound = { sound1, sound2 };
			Int16 value = BitConverter.ToInt16(sound, 0);
			return value;
		}
		// converts 
		private List<float> ConvertFormatData(byte[] Data, int Count)
		{
			List<float> Amplitudes = new List<float>();
			for (var i = 0; i < Count - 5; i += 4)
			{
				Int16 value1 = GetSampleValue(Data, i);
				Int16 value2 = GetSampleValue(Data, i+2);
				Amplitudes.Add(value2);
				Amplitudes.Add(value1);
			}
			/*
			 * Custom Operations
			 */
			float max = 0;
			for(int i = 0; i < Amplitudes.Count; i++)
			{
				if (max < Amplitudes[i])
				{
					max = Amplitudes[i];
				}
			}
			//if (max < 200)
			//	return null;
			for (int i = 0; i < Amplitudes.Count; i++)
			{
				Amplitudes[i] /= max;
				Amplitudes[i] *= 200.0f; 
			}

			return Amplitudes;
		}
		private void Normalize(ref List<float> Amplitudes, float Roof)
		{
			float Max;
			// Finds Max
			{
				float max = 0;
				for (int i = 0; i < Amplitudes.Count; i++)
				{
					if (Amplitudes[i] > max)
					{
						max = Amplitudes[i];
					}
				}
				Max = max;
			}
			// Normalizes
			for (int i = 0; i < Amplitudes.Count; i++)
			{
				Amplitudes[i] = Amplitudes[i] * Roof / Max;
			}
		}
		private int Algorithm1_SFT(List<float> Amplitudes) // stas fürer transform
		{
			if (Amplitudes == null)
				return 0;
			const int FrameSamples = 4410;
			int StartTime = DateTime.Now.Millisecond;
			const int FullCaptureFraction = 64;
			int MirrorSize = ((Amplitudes.Count) / FullCaptureFraction) * 10;

			// Finds First Positive amplitudes after reaching 0
			int StartIndex = 0;
			for (var i = 10; i < Amplitudes.Count; i++)
			{
				if (Amplitudes[i - 10] <= 0 && Amplitudes[i] > 0)
				{
					StartIndex = i - 10;
					break;
				}
			}
			if (StartIndex > MirrorSize)
				return 0;

			FrameWave = new List<float>(Amplitudes);
			// <Key: Frequency, Value : Amplitude>
			Dictionary<int, int> FrequencyContainer = new Dictionary<int, int>();
			Dictionary<int, int> FrequencyMatches = new Dictionary<int, int>();

			for (int Frequency = 500; Frequency > 120; Frequency -= 5)
			{
				int Match = 0;
				for (float i = 0; i < MirrorSize; i += 1.0f)
				{
					float SinusFunctionValue = 1 *
						(float)System.Math.Sin((i * Frequency / (double)FrameSamples / System.Math.PI));

					if (Amplitudes[(int)(i) + StartIndex] / SinusFunctionValue > 1.0f)
					{

						Match++;
					}


				}
				FrequencyMatches[Frequency] = Match;
			}
		


			int max = 0;
			int ReturnFrequency = 0;
			//foreach(var i in FrequencyContainer)
			//{
			//	if (i.Value > max)
			//	{
			//		max = i.Value;
			//		ReturnFrequency = i.Key;
			//	}
			//}
			foreach (var i in FrequencyMatches)
			{
				if (i.Value > max)
				{
					max = i.Value;
					ReturnFrequency = i.Key;
				}
			}

			Int16[] myArray = new short[MirrorSize + StartIndex];
			for (int s = 0; s < StartIndex; s++)
			{
				myArray[s] = 0;
			}


			for (float s = 0; s < MirrorSize; s += 1.0f)
			{
				float SinusFunctionValue =
					//FrequencyContainer[ReturnFrequency]
					100
					* (float)System.Math.Sin((s * ReturnFrequency / (double)FrameSamples / System.Math.PI));

				myArray[(int)(s) + StartIndex] = (Int16)SinusFunctionValue;
			}
			FrequencySinus = new List<Int16>(myArray);



			int EndTime = DateTime.Now.Millisecond;
			int TotalTime = EndTime - StartTime;
			return ReturnFrequency;
		}
		private float Algorithm2_DistanceBetweenExtremums(List<float> Amplitudes)
		{
			Int16 MaxVal1 = 0;
			Int16 MinVal1 = 0;
			int maxValIndex = 0;
			int minValIndex = 0;
			int State = 0;
			float Threshold = 160.0f;
			// Normalize Data first
			Normalize(ref Amplitudes, 200.0f);
			// Finds First Value above Threshold and under -Threshold
			for(int i = 0; i < Amplitudes.Count; i++)
			{
				if (Amplitudes[i] > Threshold)
				{
					maxValIndex = i;
					break;
				}
			}
			for (int i = maxValIndex; i < Amplitudes.Count; i++)
			{
				if (Amplitudes[i] < -Threshold)
				{
					minValIndex = i;
					break;
				}
			}

			float ReturnFrequency = (float)Amplitudes.Count / (float)((minValIndex - maxValIndex) * 2);
			return ReturnFrequency;
		}
		private float Algorithm3_ZeroIntersection(List<float> Amplitudes)
		{
			int IntersectionCount = 0;
			int FirstEncouter = 0, LastEncouter = 0;
			// Amount of time frames ignored between each test to remove faulty data
			const int TestDistance = 20;

			string CurrentSign = "Negative";

			for(int i = 0; i < Amplitudes.Count; i+=TestDistance)
			{
				if (CurrentSign == "Positive")
				{
					if (Amplitudes[i] < 0)
					{
						IntersectionCount++;
						CurrentSign = "Negative";
						if (FirstEncouter == 0)
							FirstEncouter = i;
						LastEncouter = i;
					}
				}
				if (CurrentSign == "Negative")
				{
					if (Amplitudes[i] > 0)
					{
						IntersectionCount++;
						CurrentSign = "Positive";
						if (FirstEncouter == 0)
							FirstEncouter = i;
						LastEncouter = i;
					}
				}
			}
			// Remainder approximation
			float NumFullWaves = IntersectionCount / 2;
			int Remainder = (Amplitudes.Count - (LastEncouter - FirstEncouter));
			float ReturnFrequency = 0;
			if (Remainder > (Amplitudes.Count / NumFullWaves)/2)
			{
				ReturnFrequency = NumFullWaves + (float)Remainder / (float)(Amplitudes.Count) * NumFullWaves;
			}
			else
			{
				ReturnFrequency = NumFullWaves - (float)Remainder / (float)(Amplitudes.Count) * NumFullWaves;
			}
			return ReturnFrequency;
		}
		private int Algorithm4_MatchMinimal(List<float> Amplitudes)
		{

			return 0;
		}
		private void ProceedData(List<float> Amplitudes)
		{
			// <value of differential>
			List<float> Differentials = new List<float>();
			// <index, Extremum (amplitute)>
			var Extremums = new Dictionary<int, float>();

			
			for (var i = 0; i < Amplitudes.Count - 200; i += 2)
			{
				Differentials.Add((short)(Amplitudes[i+5] - Amplitudes[i]));
				Amplitudes.Add(Amplitudes[i]);
			}
			FrameWave = new List<float>(Amplitudes);
			for (var i = 0; i < Amplitudes.Count / 2 - 200; i++)
			{
				if (Differentials[i] * Differentials[i + 1] < 0) // changed from neg to pos or pos to neg
				{
					Extremums[i] = Amplitudes[i];
				}
			}
			//
			int MatchIndex = 0;
			int MaxMatch = 1;
			Dictionary<int, float> CurrentContainer = new Dictionary<int, float>();
			Dictionary<int, float> LastContainer = new Dictionary<int, float>();

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
				LastContainer = new Dictionary<int, float>(Extremums);
				Extremums = new Dictionary<int, float>(CurrentContainer);
			}
			/**/

			SingleWave = new List<float>();
			for (int i = 0; i < LastContainer.ElementAt(LastContainer.Count - 1).Key; i++)
			{
				SingleWave.Add(Amplitudes[i]);
			}

			DifferentialsWave = new List<float>(Differentials);
			ExtremumsWave = new Dictionary<int, float>(Extremums);

		}
		private void sourceStream_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
		{
			int sample = e.BytesRecorded;
			/* analyze data */
			bool isOdd = true;
			CurrentBuffer = new byte[sample];
			// for drawing
			VolumeThreshHold = 200;
			/*
			 * Unfiltered sound comes in form of 2 opposing wave functions, probably has to do with stereo
			 * i Treat them all as a single sound, and therefor every odd number of result shall be negated.
			 */
			for (int i = 0; i < sample - 3; i += 2)
			{
				Int16 value = GetSampleValue(e.Buffer, i);
				if (isOdd) {
					value *= -1;
					isOdd = false;
				}
				else isOdd = true;


				byte[] newValues = BitConverter.GetBytes(value);
				CurrentBuffer[i + 0] = newValues[0];
				CurrentBuffer[i + 1] = newValues[1];

			}
			StoreInDrawingBuffer(CurrentBuffer, sample);
			//ProceedData(CurrentBuffer, sample);
			float ReturnFrequency = 0;
			ReturnFrequency = Algorithm1_SFT
				(this.ConvertFormatData(CurrentBuffer, sample));
			//ReturnFrequency = Algorithm2_DistanceBetweenExtremums
			//	(this.ConvertFormatData(CurrentBuffer, sample));
			//ReturnFrequency = Algorithm3_ZeroIntersection
			//	(this.ConvertFormatData(CurrentBuffer, sample));
			//ReturnFrequency = Algorithm4_MatchMinimal
			//	(this.ConvertFormatData(CurrentBuffer, sample));


			//int ReturnFrequency = 0;
			//this.ProceedData(this.ConvertFormatData(CurrentBuffer, sample));

			if (ReturnFrequency != 0)
			{
				textBox2.Text = ReturnFrequency.ToString();
				int DesiredFrequency = 330;
				if (ReturnFrequency == DesiredFrequency)
				{
					NXT.SetMotor(0, 0, Bluetooth.Motor.eState.On);
				}
				else
				{
					NXT.SetMotor(0, (DesiredFrequency - (int)ReturnFrequency), Bluetooth.Motor.eState.On);
				}
			}


			this.pictureBox1.Invalidate();
			this.pictureBox2.Invalidate();
			this.pictureBox3.Invalidate();
			//this.pictureBox4.Invalidate();



			ReturnFrequency *= 10;
			textBox1.Text = ReturnFrequency.ToString();
			//textBox1.Text = Average.ToString();



			waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
			waveWriter.Flush();
			waveIn.Read(e.Buffer, 0, e.BytesRecorded);
			count++;
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
				Int16 value = (Int16)FrameWave[0];
				previousDot.Y = (int)((float)value * ScaleFactor + 100.0f);
				previousDot.X = 0;
				for (int i = 1; i < FrameWave.Count; i++)
				{

					value = (Int16)FrameWave[i];
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
			if (key == Keys.R)
			{
				NXT.SetMotor(0, 100, Bluetooth.Motor.eState.On);
				NXT.SetMotor(1, 100, Bluetooth.Motor.eState.On);
				NXT.SetMotor(2, 100, Bluetooth.Motor.eState.On);
				textBox1.Text = "R";
			}
			if (key == Keys.L)
			{
				NXT.SetMotor(0, -100, Bluetooth.Motor.eState.On);
				NXT.SetMotor(1, -100, Bluetooth.Motor.eState.On);
				NXT.SetMotor(2, -100, Bluetooth.Motor.eState.On);
				textBox1.Text = "L";

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
				float value = DifferentialsWave[0];
	
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
					float value = i.Value;

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
			FrequencySinusValue-=10;
			pictureBox1.Invalidate();
		}

		private void button3_Click_1(object sender, EventArgs e)
		{
			string Port = textBox3.Text;
			NXT.Open(Port);
		}
	}
}
