using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;



public class GpuInstancingTest : MonoBehaviour 
{


	#region Unity properties

	[SerializeField] private Transform _originalInstance;
	[SerializeField] private int _particlesCount;
    [SerializeField] private ComputeShader _compute;
	[SerializeField, Range(0.001f, 1.0f)] private float _particleScale;
    [SerializeField, Range(0.0f, 10.0f)] private float _cellSize;

	#endregion

	#region Private fields

	private bool _isToRespawn;
    private Mesh _mesh;
    private MaterialPropertyBlock _materialPropertyBlock;
    private Material _material;
    private ComputeBuffer _drawArgsBuffer;
    private readonly ComputeBuffer[] _particlesBuffer = new ComputeBuffer[2];
    private ComputeBuffer _predicatesBuffer;
    private ComputeBuffer _adressesBuffer;
    private ComputeBuffer _adressesBufferOld;
    private ComputeBuffer _histogram;
    private int _randomSphereInitPositionsKernel;
    //private int _fluidBehaviourKernel;
    //private int _sortHashSetPass0Kernel;
    //private int _sortHashSetPass1Kernel;
    private int _radixSortKernel;

    #endregion

    #region Compute configurations

    private const int THREAD_COUNT = 32;
    private int _threadGroupCount;
	private List<Particle>[] _debugParticlesBuffers;

    #endregion



    void OnValidate()
    {
        _particlesCount = Mathf.Max(THREAD_COUNT, _particlesCount & (int.MaxValue - 63));
    }


    private void OnDestroy()
    {
        _drawArgsBuffer.Release();
	    releaseFluidBuffers();
    }


	void releaseFluidBuffers()
	{
		_particlesBuffer[0].Release();
		_particlesBuffer[1].Release();
		_predicatesBuffer.Release();
		_adressesBuffer.Release();
		_adressesBufferOld.Release();
	    _histogram.Release();
	}


    private void Start ()
	{
	    _mesh = _originalInstance.GetComponent<MeshFilter>().sharedMesh;
	    _material = _originalInstance.GetComponent<MeshRenderer>().sharedMaterial;

        // This property block is used only for avoiding a bug (issue #913828)
        _materialPropertyBlock = new MaterialPropertyBlock();
	    _materialPropertyBlock.SetFloat("_UniqueID", Random.value);

	    uint[] args = 
	    {
	        _mesh.GetIndexCount(0),
	        (uint)_particlesCount,
	        0u,
	        0u,
	        0u
        };

	    // Allocate the indirect draw args buffer.
	    _drawArgsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
	    _drawArgsBuffer.SetData(args);

	    _particlesBuffer[0] = new ComputeBuffer(_particlesCount, Particle.stride); // 12 because float3 in a compute shader
	    _particlesBuffer[1] = new ComputeBuffer(_particlesCount, Particle.stride); // 12 because float3 in a compute shader
	    _predicatesBuffer = new ComputeBuffer(_particlesCount, sizeof(uint)); // 12 because float3 in a compute shader
	    _adressesBuffer = new ComputeBuffer(_particlesCount, sizeof(uint)); // 12 because float3 in a compute shader
		_adressesBufferOld = new ComputeBuffer(_particlesCount, sizeof(uint)); // 12 because float3 in a compute shader
		_histogram = new ComputeBuffer(1, sizeof(uint)); // 12 because float3 in a compute shader

		// Kernels
	    _randomSphereInitPositionsKernel = _compute.FindKernel("randomSphereInitPositions");
	    _radixSortKernel = _compute.FindKernel("radixSort");
	    //_fluidBehaviourKernel = _compute.FindKernel("fluidBehaviour");
	    //_sortHashSetPass0Kernel = _compute.FindKernel("sortHashSetPass0");
	    //_sortHashSetPass1Kernel = _compute.FindKernel("sortHashSetPass1");

        _threadGroupCount = _particlesCount / THREAD_COUNT;
        
	    _material.SetMatrix("_Scale", Matrix4x4.Scale(Vector4.one * _particleScale));

        // Compute shader
	    _compute.SetFloat(ShaderPropertiesId.CellSizeInverted, 1 / _cellSize);
	    _compute.SetFloat(ShaderPropertiesId.ParticlesCount, _particlesCount);

        _compute.SetBuffer(_randomSphereInitPositionsKernel, ShaderPropertiesId.ParticlesBufferIn, _particlesBuffer[0]);
	    _compute.Dispatch(_randomSphereInitPositionsKernel, _threadGroupCount, 1, 1);
	    _compute.SetVector(ShaderPropertiesId.PivotWorldPosition, transform.position);

		Particle[] particles = new Particle[_particlesCount];
		_particlesBuffer[0].GetData(particles);

		_debugParticlesBuffers = new[]
		{
			new List<Particle>(particles),
			null
		};
	}


	private void Update()
	{
        // Comon shader
        _material.SetMatrix(ShaderPropertiesId.LocalToWorld, transform.localToWorldMatrix);
	    _material.SetMatrix(ShaderPropertiesId.WorldToLocal, transform.worldToLocalMatrix);
        _material.SetBuffer(ShaderPropertiesId.ParticlesBuffer, _particlesBuffer[0]);

        // Draw the mesh with instancing.
        Graphics.DrawMeshInstancedIndirect(
	        _mesh, 0, _material, 
	        new Bounds(transform.localPosition, transform.lossyScale * 1),
	        _drawArgsBuffer, 0, _materialPropertyBlock);
	}
	

    public void radixSort()
    {
	    //releaseFluidBuffers();

	    for (int i = 0; i < sizeof(int) * 8; i++)
	    {
		    _adressesBuffer.Release();
		    _adressesBuffer = new ComputeBuffer(_particlesCount, sizeof(uint));
		    _adressesBufferOld.Release();
		    _adressesBufferOld = new ComputeBuffer(_particlesCount, sizeof(uint));
		    _histogram.Release();
		    _histogram = new ComputeBuffer(1, sizeof(uint));

			_compute.SetInt(ShaderPropertiesId.SignificantBit, i);
            _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.ParticlesBufferIn, _particlesBuffer[0]);
            _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.ParticlesBufferOut, _particlesBuffer[1]);
	        _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.Predicates, _predicatesBuffer);
	        _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.Adresses, _adressesBuffer);
	        _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.AdressesOld, _adressesBufferOld);
	        _compute.SetBuffer(_radixSortKernel, ShaderPropertiesId.Histogram, _histogram);

            _compute.Dispatch(_radixSortKernel, _threadGroupCount, 1, 1);

	        debugStep(i);
			
	        ComputeBuffer temp = _particlesBuffer[0];
	        _particlesBuffer[0] = _particlesBuffer[1];
			_particlesBuffer[1] = temp;
        }
    }


	private void debugStep(int i)
	{
		Particle[] particlesIn = new Particle[_particlesCount];
		_particlesBuffer[0].GetData(particlesIn);

		Particle[] particlesOut = new Particle[_particlesCount];
		_particlesBuffer[1].GetData(particlesOut);

		uint[] predicates = new uint[_particlesCount];
		_predicatesBuffer.GetData(predicates);

		uint[] adresses = new uint[_particlesCount + 1];
		_adressesBuffer.GetData(adresses);

		uint[] histogram = new uint[1];
		_histogram.GetData(histogram);

		_debugParticlesBuffers[0] = new List<Particle>(particlesIn);
		_debugParticlesBuffers[1] = new List<Particle>(particlesOut);

		debugBuffer($"Significant bit: {i}", predicates, adresses, histogram, i);
	}


	private void debugBuffer(string title, IReadOnlyList<uint> p, IReadOnlyList<uint> a, IReadOnlyList<uint> h, int step)
	{
		string res = $"{DateTime.Now.Ticks.ToString()}_{h[0]}\n";

		//float rowSum = (1 + 64) / 2.0f * 64;
		step = (sizeof(int) * 8 - 1) - step % (sizeof(int) * 8);
		for (int i = 0; i < _debugParticlesBuffers[0].Count; i++)
		{
			int hashIn = _debugParticlesBuffers[0][i].hash;
			int hashOut = _debugParticlesBuffers[1][i].hash;

			string hashInBits = Convert.ToString(hashIn, 2).PadLeft(32, '0');
			string hashOutBits = Convert.ToString(hashOut, 2).PadLeft(32, '0');

			hashInBits = hashInBits.Insert(step, "<color=yellow>");
			hashInBits = hashInBits.Insert(step + 15, "</color>");

			hashOutBits = hashOutBits.Insert(step, "<color=yellow>");
			hashOutBits = hashOutBits.Insert(step + 15, "</color>");

			//rowSum -= hashOut;

			res += $"[{i.ToString().PadLeft(2, '_')}]" +
		       $"|{hashInBits}|\tpred: {p[i]}\tadr: {a[i]}\t{hashIn.ToString().PadLeft(11, '_')}\t-->\t" +
		       $"|{hashOutBits}|\t{hashOut.ToString().PadLeft(11, '_')}\n";
		}

		string color = "green";
		res = $"<color={color}>{title}\n</color>" + res;

		Debug.Log(res);
	}


	//public void simulateGravity()
	//{
	//	// Gravity move
	//	_compute.SetVector(ShaderPropertiesId.PivotWorldPosition, transform.position);
	//	_compute.SetFloat(ShaderPropertiesId.CellSize, _cellSize);

	//	_compute.SetBuffer(_fluidBehaviourKernel, ShaderPropertiesId.ParticlesBuffer, _particlesBuffer);
	//	_compute.Dispatch(_fluidBehaviourKernel, _threadGroupCount, 1, 1);
	//}


	//public void sortByHashes()
	//{
	//	for (int i = 0; i < 10000; i++)
	//	{
	//		// Sorting pass 0
	//		_compute.SetBuffer(_sortHashSetPass0Kernel, ShaderPropertiesId.ParticlesBuffer, _particlesBuffer);
	//		_compute.Dispatch(_sortHashSetPass0Kernel, _threadGroupCount, 1, 1);

	//		// Sorting pass 1
	//		_compute.SetBuffer(_sortHashSetPass1Kernel, ShaderPropertiesId.ParticlesBuffer, _particlesBuffer);
	//		_compute.Dispatch(_sortHashSetPass1Kernel, _threadGroupCount, 1, 1);

	//	}
	//}


}