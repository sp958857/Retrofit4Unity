using System;

namespace Retrofit.Utils
{
	public class RetrofitUtils
	{
		public static void ValidateServiceClass(Type service)
		{
			if (!service.IsInterface)
			{
				throw new ArgumentException("Only interface endpoint definitions are supported.");
			}
			if (service.GetInterfaces().Length > 0)
			{
				throw new ArgumentException("Interface definitions must not extend other interfaces.");
			}
		}
	}
}