using System;
using System.Collections.Generic;

namespace Bookstore.Core.Common
{
    public enum ResultError
    {
        None,
        ValidationFailed,
        Conflict,
        NotFound
    }

    // Outcome of an operation that can fail in ways callers are expected to
    // branch on (bad input, duplicate key, missing record) without paying for
    // exception-driven control flow on the expected paths. Success is derived
    // from Error so the two can never disagree.
    public class Result
    {
        public bool Success
        {
            get { return Error == ResultError.None; }
        }

        public ResultError Error { get; private set; }
        public IReadOnlyList<string> Errors { get; private set; }

        protected Result(ResultError error, IReadOnlyList<string> errors)
        {
            Error = error;
            Errors = errors;
        }

        public static Result Ok()
        {
            return new Result(ResultError.None, new string[0]);
        }

        public static Result Fail(ResultError error, IReadOnlyList<string> errors)
        {
            requireFailure(error);
            return new Result(error, errors);
        }

        public static Result Fail(ResultError error, params string[] errors)
        {
            requireFailure(error);
            return new Result(error, errors);
        }

        // A Result built with Error = None but failure intent is a programmer
        // error, so it fails loudly at construction instead of reading as a
        // success that skips every failure branch downstream.
        protected static void requireFailure(ResultError error)
        {
            if (error == ResultError.None)
                throw new ArgumentException(
                    "A failed Result requires an error kind other than None.", "error");
        }
    }

    // A Result that also carries a value when it succeeds (e.g. a lookup).
    public class Result<T> : Result
    {
        public T Value { get; private set; }

        private Result(T value, ResultError error, IReadOnlyList<string> errors)
            : base(error, errors)
        {
            Value = value;
        }

        public static Result<T> Ok(T value)
        {
            return new Result<T>(value, ResultError.None, new string[0]);
        }

        public new static Result<T> Fail(ResultError error, params string[] errors)
        {
            requireFailure(error);
            return new Result<T>(default(T), error, errors);
        }
    }
}
