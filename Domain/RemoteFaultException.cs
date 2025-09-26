namespace FOLKv2ws.Domain;

/// <summary>
/// Represents a normalized remote SOAP/WCF business fault returned by the CRS service.
/// </summary>
public sealed class RemoteFaultException : Exception
{
    /// <summary>Gets remote fault code value.</summary>
    public string FaultCode { get; }
    /// <summary>Gets remote fault descriptive string.</summary>
    public string? FaultString { get; }
    /// <summary>Gets operation name which produced the fault.</summary>
    public string Operation { get; }

    /// <summary>Initializes a new instance of the <see cref="RemoteFaultException"/> class with remote details.</summary>
    public RemoteFaultException(string operation, string faultCode, string? faultString)
        : base($"Remote fault in {operation}: {faultCode} - {faultString}")
    {
        Operation = operation;
        FaultCode = faultCode;
        FaultString = faultString;
    }
}
