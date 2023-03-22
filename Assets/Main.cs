using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Main : MonoBehaviour
{
    static private int system_width = 20;
    static private int system_height = 20;
    static private int system_depth = 20;
    static private int attraction_radius = 3;
    static private float attraction_strength = 0.14f;
    static private float external_pressure = 0.02f;

    private int system_voxel_count;

    

    private string[] field;
    

    private GameObject[] particles;
    private List<GameObject> particles_buffer;

    private Vector3 center;

    private Bounds system_box;


    // Start is called before the first frame update
    void Start()
    {
        system_voxel_count = system_width * system_height * system_depth;

        system_box = new Bounds(new Vector3(system_width/2,system_height/2,system_depth/2),new Vector3(system_width,system_height,system_depth));

        field = new string[system_voxel_count];
        particles_buffer = new List<GameObject>();


        center = new Vector3(system_width/2,system_height/2,system_depth/2);

        Mesh cageMesh = GameObject.Find("FieldMesh").GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = cageMesh.vertices;
        Vector3 disp = new Vector3(system_width,system_height,system_depth);
        for(int i=0; i<vertices.Length; i++){
            vertices[i] = vertices[i] * system_width / 2 + disp / 2;
        }

        cageMesh.vertices = vertices;

        System.Random rnd = new System.Random();
        
        int id_iterator = 0;
        for(int i=0; i<system_voxel_count; i++){
            // percentage chance of generating a particle at the given voxel.
            if(rnd.Next(0,100) < 10){
                String name = String.Format("Particle{0}",id_iterator);
                GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Element e_property = p.AddComponent<Element>();
                e_property.id = id_iterator;
                Rigidbody rb = p.AddComponent<Rigidbody>();
                Transform tr = p.GetComponent<Transform>();
                Material m = p.GetComponent<Renderer>().material;

                List<int> proportions = new List<int>();
                proportions.Add(0);
                for(int n=0; n<10; n++){proportions.Add(1);}
                for(int n=0; n<25; n++){proportions.Add(2);}
                int selection = proportions[rnd.Next(0,proportions.Count)];
                if(selection == 0){
                    float scale = 2.0f;
                    Vector3 extents = new Vector3(scale/2,scale/2,scale/2);
                    Vector3 proposed_pos = reverseIndex(i) + extents;
                    if(system_box.Contains(proposed_pos - extents) && system_box.Contains(proposed_pos + extents)){
                        m.color = new Color(0.5f,0.5f,0.8f);
                        tr.localScale = tr.localScale * scale;
                        e_property.charge = -3;
                    }else{
                        selection = 1;
                    }

                    
                    
                }
                if(selection == 1){
                    m.color = new Color(0.8f,0.5f,0.5f);
                    e_property.charge = 2;
                }
                if(selection == 2){
                    m.color = new Color(0.5f,0.5f,0.8f);
                    tr.localScale = tr.localScale * 0.5f;
                    e_property.charge = -1;
                }
                tr.position = reverseIndex(i) + tr.localScale/2;
                rb.mass = 5*3.1415f*(float)Math.Pow(tr.localScale.x/2,3);

                p.name = name;
                particles_buffer.Add(p);
                id_iterator++;

            }
        }
        
        particles = particles_buffer.ToArray();
    }


    // Update is called once per frame
    void Update()
    {
        for(int i=0; i<field.Length; i++){
            field[i] = "";
        }
        
        for(int i=0; i<particles.Length; i++){
            Transform tr = particles[i].GetComponent<Transform>();
            Rigidbody rb = particles[i].GetComponent<Rigidbody>();

            if(tr.position.x + tr.localScale.x/2 >= system_width){
                tr.position = new Vector3(system_width - tr.localScale.x/2,tr.position.y,tr.position.z);
                rb.velocity = new Vector3(rb.velocity.x*-1,rb.velocity.y,rb.velocity.z);
            }
            if(tr.position.y + tr.localScale.y/2 >= system_height){
                tr.position = new Vector3(tr.position.x,system_height - tr.localScale.y/2,tr.position.z);
                rb.velocity = new Vector3(rb.velocity.x,rb.velocity.y*-1,rb.velocity.z);
            }
            if(tr.position.z + tr.localScale.z/2 >= system_depth){
                tr.position = new Vector3(tr.position.x,tr.position.y,system_depth-tr.localScale.z/2);
                rb.velocity = new Vector3(rb.velocity.x,rb.velocity.y,rb.velocity.z*-1);
            }
            if(tr.position.x - tr.localScale.x/2 <= 0){
                tr.position = new Vector3(tr.localScale.x/2,tr.position.y,tr.position.z);
                rb.velocity = new Vector3(rb.velocity.x*-1,rb.velocity.y,rb.velocity.z);
            }
            if(tr.position.y - tr.localScale.y/2 <= 0){
                tr.position = new Vector3(tr.position.x,tr.localScale.y/2,tr.position.z);
                rb.velocity = new Vector3(rb.velocity.x,rb.velocity.y*-1,rb.velocity.z);
            }
            if(tr.position.z - tr.localScale.z/2 <= 0){
                tr.position = new Vector3(tr.position.x,tr.position.y,tr.localScale.z/2);
                rb.velocity = new Vector3(rb.velocity.x,rb.velocity.y,rb.velocity.z*-1);
            }
            
            List<int> vol = getVolumeIndex(tr.position);
            
            for(int n=0; n<vol.Count; n++){
                if(field[vol[n]] == ""){
                    continue;
                }
                int[] elements = parse(field[vol[n]].Split(","));
                
                for(int m=0; m<elements.Length; m++){
                    if(elements[m] == i){
                        continue;
                    }
                    GameObject e1 = particles[i];
                    GameObject e2 = particles[elements[m]];

                    Transform tr2 = e2.GetComponent<Transform>();
                    Rigidbody rb2 = e2.GetComponent<Rigidbody>();

                    Vector3 vector = tr2.position - tr.position;
                    float force = -1 * attraction_strength * e1.GetComponent<Element>().charge * e2.GetComponent<Element>().charge / vector.sqrMagnitude;
                    float contact_dist = tr.localScale.x /2 + tr2.localScale.x /2;
                    if((float)vector.magnitude - contact_dist < 0.0005f){
                        force*=1.6f;

                        rb.velocity*=0.98f;
                        rb2.velocity*=0.98f;
                    }
                    rb.velocity = rb.velocity + vector.normalized * force / rb.mass;
                    rb2.velocity = rb2.velocity + -1 * vector.normalized * force / rb2.mass;
                }
            }

            if(Math.Abs(external_pressure)>0.00001){
                Vector3 toCenter = center - tr.position;
                rb.velocity = rb.velocity + toCenter.normalized * external_pressure;
            }
            
            
            int index_pos = index(tr.position);
            if(field[index_pos] != ""){
                field[index_pos] +=",";
            }
            field[index_pos]+=i.ToString();
            
        }
    }

    private int[] parse(string[] e)
    {
        int[] output = new int[e.Length];
        for(int i=0; i<e.Length; i++){
            output[i] = int.Parse(e[i]);
        }
        return output;
    }

    private int index(int x,int y,int z)
    {
        return z * system_width * system_height + y * system_width + x;
    }
    private int index(Vector3 pos)
    {
        int x = (int)Math.Floor(pos.x);
        int y = (int)Math.Floor(pos.y);
        int z = (int)Math.Floor(pos.z);
        return z * system_width * system_height + y * system_width + x;
    }
    private List<int> getVolumeIndex(Vector3 pos)
    {
        List<int> output = new List<int>();
        int xp = (int)Math.Floor(pos.x);
        int yp = (int)Math.Floor(pos.y);
        int zp = (int)Math.Floor(pos.z);
        for(int i=xp-attraction_radius+1; i<xp+attraction_radius; i++){
            if(i<0 || i>system_width-1){
                continue;
            }
            for(int j=yp-attraction_radius+1; j<yp+attraction_radius; j++){
                if(j<0 || j>system_height-1){
                    continue;
                }
                for(int k=zp-attraction_radius+1; k<zp+attraction_radius; k++){
                    if(k<0 || k>system_depth-1){
                        continue;
                    }
                    output.Add(k * system_width * system_height + j * system_width + i);

                }
            }
        }
        return output;
    }

    private int[] GetRow(int[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }

    private Vector3 reverseIndex(int i)
    {
        Vector3 pos = new Vector3(0,0,0);
        pos.z = (float)(i / (system_width * system_height));
        pos.y = (float)((i % (system_width*system_height)) / system_width);
        pos.x = (float)(i % system_width);
        return pos;
    }

}
