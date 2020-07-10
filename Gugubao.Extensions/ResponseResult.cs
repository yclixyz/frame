using Microsoft.AspNetCore.Mvc;
using System;

namespace Gugubao.Extensions
{
    /// <summary>
    /// 返回格式
    /// </summary>
    /// <typeparam name="T">返回数据</typeparam>
    public class ResponseValue<T>
    {
        public ResponseValue()
        {
            CreateTime = DateTime.Now;
            Data = default;
            Success = true;
            ErrorMsg = string.Empty;
        }

        public ResponseValue(T value)
        {
            CreateTime = DateTime.Now;
            Data = value;
            Success = true;
            ErrorMsg = string.Empty;
        }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 返回数据
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// 返回时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 返回数据
    /// </summary>
    public class ResponseValue : ResponseValue<string>
    {
    }

    /// <summary>
    /// Api通用返回结果，不包含数据
    /// </summary>
    public class ResponseResult : JsonResult
    {
        public ResponseResult() : base(new ResponseValue())
        {

        }
    }

    /// <summary>
    /// Api通用返回结果，包含数据
    /// </summary>
    /// <typeparam name="T">返回的Data数据类型</typeparam>
    public class ResponseResult<T> : JsonResult
    {
        public ResponseResult(T data) : base(new ResponseValue<T>(data))
        {
        }
    }
}
