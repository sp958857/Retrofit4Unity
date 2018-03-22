#region License
// Author: Weichao Wang     
// Start Date: 2017-05-22
#endregion
using System;

namespace Retrofit
{
    public class ValueAttribute : Attribute
    {
        public string Value { get; protected set; }
    }
}