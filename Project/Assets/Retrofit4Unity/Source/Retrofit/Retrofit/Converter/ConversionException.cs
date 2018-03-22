#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System;

namespace Retrofit.Converter
{
    public class ConversionException : Exception
    {
        public ConversionException(string message):base(message)
        {
        }
    }
}