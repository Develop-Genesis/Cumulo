using System;
using System.Collections.Generic;
using System.Text;

namespace Cumulo.Exceptions
{
    public class VersionNotAccurateException : Exception
    {
        public VersionNotAccurateException() : base("The version of the snapshoot is not accurate")
        {

        }
    }
}
