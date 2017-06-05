using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace winformsAudio
{
	class Bluetooth
	{
		System.IO.Ports.SerialPort serialPort;

		public int send(List<byte> message)
		{
			//sends a message to the RCX
			//returns 1 if write succeeded, 0 if failed
			//maximal length of message is 125 bytes
			int i;
			long res;

			try
			{
				serialPort.Write(message.ToArray(), 0, message.Count);
			}
			catch(Exception e)
			{
				Console.Write(e.ToString());
			}
				return 0;
		}
		public int receive(ref List<byte> rcbuf)
		{
			const int size = 1024;
			byte[] buffer = new byte[size];
			int numBytesRead = serialPort.Read(buffer, 0, size);
			string output = Convert.ToString(buffer);
			rcbuf = new List<byte>(buffer);
			return numBytesRead;
		}
		public int close()
		{
			serialPort.Close();
			return 0;
		}
		string nxtname;
		string btaddress;
		public int Open(string portname)
		{
			serialPort = new System.IO.Ports.SerialPort(portname);
			serialPort.ReadTimeout = 500;
			serialPort.WriteTimeout = 500;
			serialPort.Open();
			// initialize communication
			List<byte> bytes = new List<byte>();
			bytes.Add(2); //two bytes follow
			bytes.Add(0);
			bytes.Add(0x01);
			bytes.Add(0x9B);
			int res = send(bytes);
			//if (res == 0)
			//{
			//	close();
			//	return (0);
			//}
			System.Threading.Thread.Sleep(500); //wait 100ms
			List<byte> message = null;
			res = receive(ref message);
			bytes = message;
			if ((res != 35) || (bytes[2] != 0x02) || (bytes[3] != 0x9b) || (bytes[4] != 0x00))
			{ //correct response?
				close();
				return (0);
			}

			//store device info

			//nxtname = System.Text.Encoding.Default.GetString(bytes.ToArray(), 5, 15);
			//btaddress = System.Text.Encoding.Default.GetString(bytes.ToArray(), 20, 7);

			return 1;
		}
		public void SetMotor(int Port, int Value)
		{
			motors[Port].Value = Value;
			UpdateMotors(Port);
		}
		public void SetMotor(int Port, int Value, Motor.eState State)
		{
			motors[Port].Value = Value;
			motors[Port].State = State;
			UpdateMotors(Port);
		}
		void UpdateMotors(int Port)
		{
			//Set motor output values, if changed
		
			if (motors[Port].Value < -100) { motors[Port].Value = -100; }
			if (motors[Port].Value > 100) { motors[Port].Value = 100; }
			if (motors[Port].State == Motor.eState.On)
			{ //motor on
				SetOutputState((byte)Port, (byte)motors[Port].Value, 5, 0x01, 0x00, 0x20, 0);
			}
			else
			{ //motor off
				SetOutputState((byte)Port, (byte)motors[Port].Value, 0, 0x00, 0x00, 0x00, 0);
			}
		
		}
		public struct Motor
		{
			public enum eState
			{
				On, Off
			}
			public eState State;
			public int Value;
		}
		Motor[] motors = new Motor[3];

		int SetOutputState(byte outputnr, byte powersetpoint,
	byte modebyte, byte regulationmode, byte turnratio,
	byte runstate, long tacholimit)
		{
			int res;
			List<byte> bytes = new List<byte>();
			if ((outputnr < 0) || (outputnr > 2)) return (0);

			bytes.Add(0x0C); //message length without first two bytes: 13 bytes follow (0x0c)
			bytes.Add(0x00);
			bytes.Add(0x80); //no response please
			bytes.Add(0x04); //SETOUTPUTSTATE
			bytes.Add(outputnr); //port number 0..2
			bytes.Add((byte)powersetpoint);
			bytes.Add(modebyte);
			bytes.Add(regulationmode);
			bytes.Add(turnratio);
			bytes.Add(runstate);
			bytes.Add((byte)(tacholimit & 0xff)); //unsigned long tacholimit -not sure if the byte order is correct
			bytes.Add((byte)((tacholimit >> 8) & 0xff));
			bytes.Add((byte)((tacholimit >> 16) & 0xff));
			bytes.Add((byte)(tacholimit >> 24));
			res = this.send(bytes); // 12,0,128,4,1,100,5,1,0,32,0,0,0,0
			//if (res == 0) return (0);

			/*res=NXT_receive(mess,3+2);
			if ((res!=5)||(mess[4]!=0)) return(0);*/

			return (1);
		}
	}
}
