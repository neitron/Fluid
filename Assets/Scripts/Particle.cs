using UnityEngine;



public struct Particle
{


    public int hash;
    public Vector3 position;



    public static int stride => sizeof(int) + sizeof(float) * 3;


}