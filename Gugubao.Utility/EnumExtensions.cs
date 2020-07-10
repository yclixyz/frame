using System.ComponentModel;
using System.Reflection;

namespace Gugubao.Utility
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举的备注
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">枚举值</param>
        /// <returns>返回枚举的备注值</returns>
        public static string GetDescription<TEnum>(this TEnum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var descriptionAttribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();

            return descriptionAttribute?.Description;
        }
    }
}
