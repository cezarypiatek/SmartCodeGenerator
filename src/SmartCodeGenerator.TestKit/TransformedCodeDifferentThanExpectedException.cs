using System;

namespace SmartCodeGenerator.TestKit
{
    public class TransformedCodeDifferentThanExpectedException : Exception
    {
        public string Diff { get; }
        public string TransformedCode { get; }
        public string ExpectedCode { get; }

        public TransformedCodeDifferentThanExpectedException(string transformedCode, string expectedCode, string diff)
            : base($"Transformed code is different than expected:{Environment.NewLine}{diff}")
        {
            Diff = diff;
            TransformedCode = transformedCode;
            ExpectedCode = expectedCode;
        }
    }
}