using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace winformsAudio
{
	class Math
	{
		public struct Result {
			public bool To_Extent(double offset)
			{
				if (System.Math.Abs( this.value ) - offset < 0)
					return true;
				else
					return false;
			}
			public bool Precise()
			{
				if (value == 0)
					return true;
				else return false;
			}
			public double value;
		}

		public static Result IsEqual(double Num1, double Num2)
		{
			Result result = new Result();
			result.value = Num1 - Num2;
			return result;
		}
	}
}
