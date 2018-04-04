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
			// Prevent API interfaces from extending other interfaces. This not only avoids a bug in
			// Android (http://b.android.com/58753) but it forces composition of API declarations which is
			// the recommended pattern.
			if (service.GetInterfaces().Length > 0)
			{
				throw new ArgumentException("Interface definitions must not extend other interfaces.");
			}
		}
	}
}