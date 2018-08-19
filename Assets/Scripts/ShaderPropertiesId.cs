using UnityEngine;



public struct ShaderPropertiesId
{


    public static readonly int ParticlesBufferIn;
    public static readonly int ParticlesBufferOut;
    public static readonly int ParticlesBuffer;
    public static readonly int LocalToWorld;
    public static readonly int WorldToLocal;
    public static readonly int PivotWorldPosition;
    public static readonly int CellSizeInverted;
    public static readonly int ParticlesCount;
    public static readonly int Predicates;
    public static readonly int Adresses;
    public static readonly int AdressesOld;
    public static readonly int SignificantBit;
    public static readonly int Histogram;



    static ShaderPropertiesId()
    {
        ParticlesBufferIn = Shader.PropertyToID("_ParticlesBufferIn");
        ParticlesBufferOut = Shader.PropertyToID("_ParticlesBufferOut");
        ParticlesBuffer = Shader.PropertyToID("_ParticlesBuffer");
        LocalToWorld = Shader.PropertyToID("_LocalToWorld");
        WorldToLocal = Shader.PropertyToID("_WorldToLocal");
        PivotWorldPosition = Shader.PropertyToID("_PivotWorldPosition");
	    CellSizeInverted = Shader.PropertyToID("_CellSizeInverted");
        ParticlesCount = Shader.PropertyToID("_ParticlesCount");
	    Predicates = Shader.PropertyToID("predicates");
	    Adresses = Shader.PropertyToID("adresses");
	    AdressesOld = Shader.PropertyToID("adressesOld");
	    SignificantBit = Shader.PropertyToID("significantBit");
	    Histogram = Shader.PropertyToID("_Histogram");
    }


}