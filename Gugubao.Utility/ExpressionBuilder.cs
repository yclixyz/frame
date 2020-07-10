using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;

namespace Gugubao.Utility
{
    /// <summary>
    /// 生成动态表达式树,匿名对象字段要和数据库实体对应
    /// </summary>
    /// <typeparam name="T">返回的数据库实体类型</typeparam>
    public class ExpressionBuilder<T>
    {
        /// <summary>
        /// 生成And表达书树c=>c.Id==1 && c.Name="test"
        /// </summary>
        /// <param name="dto">匿名查询对象，需要和实体对应</param>
        /// <returns>Expression<Func<T,bool>></returns>
        public Expression<Func<T, bool>> AndOr(object dto, string opera = "&&")
        {
            var paras = BuildDic(dto);

            StringBuilder sb = new StringBuilder();

            var fieldNames = paras.Keys.ToList();

            for (int i = 0; i < fieldNames.Count; i++)
            {
                sb.Append(fieldNames[i]).AppendFormat("== @{0}{1}", i, opera);
            }

            var lambda = sb.ToString();

            if (lambda == "")
            {
                return x => 1 == 1;
            }

            lambda = lambda.Substring(0, lambda.Length - opera.Length);

            return DynamicExpressionParser.ParseLambda<T, bool>(new ParsingConfig(), false, lambda, paras.Values.ToArray());
        }

        /// <summary>
        /// 生成And、Contains表达书树c=>c.Id==1 && c.Name.Contains("test")
        /// </summary>
        /// <param name="eEntity">需要比较相等的对象</param>
        /// <param name="cEntity">模糊查询对象</param>
        /// <returns>Expression<Func<T,bool>></returns>
        public Expression<Func<T, bool>> Contains(object eEntity, object cEntity, string opera = "&&")
        {
            var equals = BuildDic(eEntity);

            var contains = BuildDic(cEntity);

            StringBuilder sb = new StringBuilder();

            var eNames = equals.Keys.ToList();

            for (int i = 0; i < eNames.Count; i++)
            {
                sb.Append(eNames[i]).AppendFormat("== @{0} {1}", i, opera);
            }

            var cNames = contains.Keys.ToList();

            for (int i = 0; i < cNames.Count; i++)
            {
                sb.Append(cNames[i]).AppendFormat(".Contains(@{0}) {1}", i + eNames.Count, opera);
            }

            var lambda = sb.ToString();

            if (lambda == "")
            {
                return x => 1 == 1;
            }

            lambda = lambda.Substring(0, lambda.Length - opera.Length);

            var paras = equals.Union(contains).ToDictionary(pair => pair.Key, pair => pair.Value);

            return DynamicExpressionParser.ParseLambda<T, bool>(new ParsingConfig(), false, lambda, paras.Values.ToArray());
        }

        /// <summary>
        /// 构建查询数据
        /// </summary>
        /// <param name="entity">查询参数</param>
        /// <returns>查询参数字典</returns>
        private Dictionary<string, object> BuildDic(object entity)
        {
            var dic = new Dictionary<string, object>();

            if (entity == null)
            {
                return dic;
            }

            entity.GetType().GetProperties().ToList().ForEach(c =>
            {
                var propertyValue = c.GetValue(entity);

                if (propertyValue != null && propertyValue.ToString() != "" && !dic.ContainsKey(c.Name))
                {
                    // 枚举传递All查询所有
                    if (c.PropertyType.IsEnum)
                    {
                        if (propertyValue.ToString() != "All")
                        {
                            dic.Add(c.Name, propertyValue);
                        }
                    }
                    else
                    {
                        // 如果是int型 切值为-1 查询所有
                        if (int.TryParse(propertyValue.ToString(), out int result) && result == -1)
                        {
                            return;
                        }
                        else
                        {
                            dic.Add(c.Name, propertyValue);
                        }
                    }
                }
            });

            return dic;
        }
    }
}
