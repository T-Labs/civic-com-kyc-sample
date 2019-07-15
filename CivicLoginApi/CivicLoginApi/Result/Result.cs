namespace CivicLoginApi.Result
{
    public class Result
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public static Result Error(string error)
        {
            return new Result
            {
                IsSuccess = false,
                Message = error,
            };
        }

        public static Result Success(string message = "success")
        {
            return new Result
            {
                IsSuccess = true,
                Message = message
            };
        }
    }
    public class Result<T> : Result
    {
        public T Data { get; set; }

        public static Result<T> Success(T data, string message = "success")
        {
            return new Result<T>
            {
                IsSuccess = true,
                Data = data,
                Message = message
            };
        }

        public new static Result<T> Error(string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message = error,
            };
        }

        public static Result<T> Error(T data, string error)
        {
            return new Result<T>
            {
                IsSuccess = false,
                Message = error,
            };

        }
    }

}
