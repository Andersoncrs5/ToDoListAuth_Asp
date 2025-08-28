using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListJwt.utils
{
    public class ResponseBody<T>
    {
        public string? Url { get; set; }
        public string? Message { get; set; }
        public int? StatusCode { get; set; }
        public T? Body { get; set; }
        public bool? Success  { get; set; }
        public string[]? Links { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}