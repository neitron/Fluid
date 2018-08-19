using UnityEngine;



public class Test : MonoBehaviour
{


	[SerializeField] private ComputeShader _compute;


	private ComputeBuffer _testCb;



	private void OnDestroy()
	{
		_testCb.Release();
	}


	void Start ()
	{
		int testK =_compute.FindKernel("Test");

		_testCb = new ComputeBuffer(8, 8 * 3 * sizeof(uint));

		_compute.SetBuffer(testK, "_RWStructuredBuffer", _testCb);
		_compute.Dispatch(testK, 8, 1, 1);

		Vector3[] bd = new Vector3[8];
		_testCb.GetData(bd);
				
		debugArray(bd, "Start Data");
	}


	private static void debugArray(Vector3[] bd, string title)
	{
		string res = $"{title}\n";

		for (int i = 0; i < 8; i++)
		{
			res += $"{bd[i].ToString("F3")}\n";
		}

		Debug.Log(res);
	}


}
