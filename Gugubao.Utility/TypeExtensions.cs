using System;
using System.Collections.Generic;
using System.Text;

namespace Gugubao.Utility
{
    public static class TypeExtensions
    {
        public static long StringToInt64(this string @object)
        {
            if (long.TryParse(@object, out long value))
            {
                return value;
            }

            return 0;
        }
        
    }
}
