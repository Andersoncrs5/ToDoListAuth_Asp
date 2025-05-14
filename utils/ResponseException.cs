using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TodoListJwt.utils
{
    public class ResponseException : Exception
    {
        public int StatusCode { get; set; }
        public string Status { get; set; }

        public ResponseException(string message, int statusCode = 400, string status = "success") : base(message)
        {
            StatusCode = statusCode;
            Status = status;
        }
    }

}