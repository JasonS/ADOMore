namespace ADOMore.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public sealed class TestClass
    {
        public Guid SetGuid { get; set; }
        public Guid SetNullGuid { get; set; }
        public bool SetBool { get; set; }
        public bool? SetNullBool { get; set; }
        public string SetString { get; set; }
        public char SetChar { get; set; }
        public char? SetNullChar { get; set; }
        public Int16 SetInt16 { get; set; }
        public Int32 SetInt32 { get; set; }
        public Int32? SetNullInt32 { get; set; }
        public Int64 SetInt64{ get; set; }
        public Single SetSingle { get; set; }
        public Single? SetNullSingle { get; set; }
        public double SetDouble { get; set; }
        public double? SetNullDouble { get; set; }
        public decimal SetDecimal { get; set; }
        public decimal? SetNullDecimal { get; set; }
        public DateTime SetDateTime { get; set; }
        public DateTime? SetNullDateTime { get; set; }
        public TestType SetTestType { get; set; }
        public TestType? SetNullTestType { get; set; }
    }
}
