using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;
using UnityStandardAssets.Characters.ThirdPerson;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ThirdPersonCharacter))]
public class Brain : MonoBehaviour, ITestForTarget
{
    [Serializable]
    public class DNAGroups
    {
        public DNA _movementDNAForwardBackward;
        public DNA _heightDNA;
        public DNA _movementDNALeftRight;
        public DNA _movementDNATurn;

        public DNA _priorityDNA;

        public DNAGroups Clone() => new DNAGroups() {
            _movementDNAForwardBackward = DNA.Clone(_movementDNAForwardBackward),
            _heightDNA = DNA.Clone(_heightDNA),
            _movementDNALeftRight = DNA.Clone(_movementDNALeftRight),
            _movementDNATurn = DNA.Clone(_movementDNATurn),
            _priorityDNA = DNA.Clone(_priorityDNA)
        };
    }
    public float timeAlive;
    public DNAGroups dnaGroups;
    private ThirdPersonCharacter _character;
    private Vector3 _move;
    private bool _alive = true;
    public static event Action Dead;
    private Vector3 startPos;
    private Vector3 endPos;
    [SerializeField] private GameObject[] turnOffOnDeath;
    [SerializeField] private string[] tagsToLookFor;
    private List<Color> _coloursOfTaggedItems = new List<Color>();
    [SerializeField] private string[] tagsForDie;
    [SerializeField] private GameObject lightBulb;

    public float GetProgress()
    {
        if (_alive)
        {
            // Vector3 vector31 = new Vector3(0, 0, startPos.z);
            // Vector3 vector32 = new Vector3(0, 0, transform.position.z);
            // return Vector3.Distance(vector31, vector32);
            float distance = transform.position.z - startPos.z;
            if (distance > 500)
            {
                _alive = false;
                return 0f;
            }
            return distance;
            
        }
        return 0f;
    }
    public float Distance
    {
        get => GetDistanceTraveled();
    }

    private void Awake()
    {
        foreach (string tag in tagsToLookFor)
        {
            if (tag == "Ethan")
            {
                _coloursOfTaggedItems.Add(new Color(0.29f, 0.09f, 0.5f));
                continue;
            }
            try
            {
                _coloursOfTaggedItems.Add(GameObject.FindGameObjectWithTag(tag).GetComponent<MeshRenderer>().material.color);
            }
            catch (Exception e)
            {
                _coloursOfTaggedItems.Add(Color.grey);
                Console.WriteLine(e);
            }
        }
        dnaGroups._movementDNAForwardBackward = new DNA((tagsToLookFor.Length * 4) + 1, 3, "MovementForwardBackward");
        dnaGroups._movementDNALeftRight = new DNA((tagsToLookFor.Length * 4) + 1, 3, "MovementLeftRight");
        dnaGroups._movementDNATurn = new DNA((tagsToLookFor.Length * 4) + 1, 3, "MovementForwardBackward");
        dnaGroups._heightDNA = new DNA((tagsToLookFor.Length * 4) + 1, 3, "Height");
        dnaGroups._priorityDNA = new DNA((tagsToLookFor.Length * 4), 100, "Priority");
    }

    public string GetDNAString()
    {
        return JsonConvert.SerializeObject(dnaGroups);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!_alive) return;
        if (tagsForDie.Contains(other.gameObject.tag))
        {
            _alive = false;
            Dead?.Invoke();
            SetEndPosition();
            DeathOnOff(false);
        }
    }

    public void DeathOnOff(bool on)
    {
        foreach (GameObject gObject in turnOffOnDeath)
        {
            gObject.SetActive(on);
        }
    }

    private void SetEndPosition()
    {
        endPos = transform.position;
    }

    public float GetDistanceTraveled()
    {
        if (_alive) SetEndPosition();
        float distance = endPos.z - startPos.z;
        if (distance > 500)
        {
            return 0f;
        }
        return distance;
    }

    public void Init()
    {
        startPos = transform.position;
        startPos = transform.position;
        _move = transform.position;
        _character = GetComponent<ThirdPersonCharacter>();
        timeAlive = 0;
        _alive = true;
    }

    private void moveFromDNAValue(int movementForwardBackwardValue,int heightValue, int movementLeftRight, int movementTurn)
    {
        float v = 0f;
        float h = 0f;
        bool crouch = false;
        bool jump = false;
        float r = 0;

        switch (movementForwardBackwardValue)
        {
            case 0: //  -VAL 1 = STOP
                v = 0;
                break;
            case 1: //  -VAL 2 = TURN LEFT
                v = 1;
                break;
            case 2: //  -VAL 3 = TURN RIGHT
                v = -1;
                break;
        }
        switch (movementLeftRight)
        {
            case 0: //  -VAL 1 = STOP
                h = 0;
                break;
            case 1: //  -VAL 2 = TURN LEFT
                h = 1;
                break;
            case 2: //  -VAL 3 = TURN RIGHT
                h = -1;
                break;
        }
        switch (movementTurn)
        {
            case 0: //  -VAL 1 = STOP
                //r = 0;
                break;
            case 1: //  -VAL 2 = TURN LEFT
                //r = 90;
                break;
            case 2: //  -VAL 3 = TURN RIGHT
                //r = -90;
                break;
        }
        
        switch (heightValue)
        {
            case 0: //  -VAL 1 = NORMAL
                break;
            case 1: //  -VAL 2 = CROUCH
                crouch = true;
                break;
            case 2: //  -VAL 3 = JUMP
                jump = true;
                break;
        }
        _character.transform.rotation =
            Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, r, 0), Time.time * 0.1f);
        _move = v * Vector3.forward + h * Vector3.right;
        _character.Move(_move,crouch,jump);
    }


    private void FixedUpdate()
    {
        if (!_alive) return;
        List<GameObject> seen = new List<GameObject>();
        List<Vector3> vector3s = new List<Vector3>();
        Vector3 eye = new Vector3();
        foreach (FieldOfView fieldOfView in GetComponents<FieldOfView>())
        {
            eye = fieldOfView.Eye.transform.position;
            (List<GameObject> gameObjects, List<Vector3> objectPositions) = fieldOfView.FindVisibleTargets(this);
            seen.AddRange(gameObjects);
            vector3s.AddRange(objectPositions);
        }
        MakeDecision(seen, eye, vector3s);
        if (_alive) timeAlive += Time.deltaTime;

    }

    (bool, GameObject) FindClosestTag(string tag)
    {
        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        float distance = Mathf.Infinity;
        bool found = false;
        Vector3 position = transform.position;
        foreach (GameObject go in gos)
        {
            found = true;
            Vector3 diff = go.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closest = go;
                distance = curDistance;
            }
        }
        return (found, closest);
    }
    private void MakeDecision(List<GameObject> seen, Vector3 eye, List<Vector3> pos)
    {
        int moveFB = 0;
        int height = 0;
        int moveLR = 0;
        int moveT = 0;
        Vector3 lightBulbPosition = lightBulb.transform.position;
        List<string> seenObjects = new List<string>();
        if (seen.Count > 0)
        {
            List<(int index, int value)> dnaPos = new List<(int, int)>();
            var vals = dnaGroups._priorityDNA.GetGenes();
            for (var index = 0; index < vals.Count; index++)
            {
                int val = vals[index];
                dnaPos.Add((index, val));
            }
            dnaPos.Sort((x, y) => y.value.CompareTo(x.value));
            List<(int index, float value, Vector3 pos)> options = new List<(int, float, Vector3)>();
            try
            {
                AddSeenObjects(seen, pos, seenObjects, options);
            }
            catch (Exception e)
            {
                Console.WriteLine($"See Options {e}");
            }

            try
            {
                AddNotSeenOptions(seenObjects, options, lightBulbPosition);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Not See Options {e}");
            }
            
            
            options.Sort((x, y) => y.value.CompareTo(x.value));
            moveFB = dnaGroups._movementDNAForwardBackward.GetGene(options[0].index);
            height = dnaGroups._heightDNA.GetGene(options[0].index);
            moveLR = dnaGroups._movementDNALeftRight.GetGene(options[0].index);
            moveT = dnaGroups._movementDNATurn.GetGene(options[0].index);
            Color selectedColour = Color.white;
            if (options[0].index >= (tagsToLookFor.Length * 3))
            {
                lightBulb.GetComponent<LightBulb>().ChangeColor(_coloursOfTaggedItems[options[0].index - (tagsToLookFor.Length * 3)]);
            }
            else
            {
                lightBulb.GetComponent<LightBulb>().ChangeColor(_coloursOfTaggedItems[options[0].index % tagsToLookFor.Length]);
                Debug.DrawLine(eye, options[0].pos, selectedColour);
            }

        }
        else
        {
            moveFB = CantSeeAnything(eye, lightBulbPosition, out height, out moveLR, out moveT);
        }

        try
        {
            moveFromDNAValue(moveFB, height, moveLR, moveT);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Movement {e}");
            throw;
        }
        
    }

    private int CantSeeAnything(Vector3 eye, Vector3 lightBulbPosition, out int height, out int moveLR, out int moveT)
    {
        int moveFB;
        lightBulb.GetComponent<LightBulb>().ChangeColor(Color.black);
        moveFB = dnaGroups._movementDNAForwardBackward.GetGene((tagsToLookFor.Length * 4));
        height = dnaGroups._heightDNA.GetGene((tagsToLookFor.Length * 4));
        moveLR = dnaGroups._movementDNALeftRight.GetGene((tagsToLookFor.Length * 4));
        moveT = dnaGroups._movementDNATurn.GetGene((tagsToLookFor.Length * 4));
        Debug.DrawLine(eye, lightBulbPosition, Color.red);
        return moveFB;
    }

    private void AddNotSeenOptions(List<string> seenObjects, List<(int index, float value, Vector3 pos)> options, Vector3 lightBulbPosition)
    {
        for (var i = 0; i < tagsToLookFor.Length; i++)
        {
            if (!seenObjects.Contains(tagsToLookFor[i]))
            {
                int index = (tagsToLookFor.Length * 3) + i;
                float distance = Mathf.Infinity;
                (bool found, GameObject taggedObject) = FindClosestTag(tagsToLookFor[i]);
                if (found)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, taggedObject.transform.position,
                        out hit, Mathf.Infinity)) distance = Vector3.Distance(transform.position, hit.transform.position);
                }

                options.Add((index, dnaGroups._priorityDNA.GetGene(index) / distance, lightBulbPosition));
            }
        }
    }

    private void AddSeenObjects(List<GameObject> seen, List<Vector3> pos, List<string> seenObjects, List<(int index, float value, Vector3 pos)> options)
    {
        for (int index = 0; index < seen.Count; index++)
        {
            GameObject visibleObject = seen[index];
            for (var i = 0; i < tagsToLookFor.Length; i++)
            {
                string tag = tagsToLookFor[i];
                if (visibleObject.CompareTag(tag))
                {
                    int geneIndex = i;
                    seenObjects.Add(tagsToLookFor[i]);
                    float dir = FieldOfView.AngleDir(transform.forward, visibleObject.transform.position,
                        transform.up);
                    switch (dir)
                    {
                        case 0f:
                            geneIndex = i;
                            break;
                        case -1f:
                            geneIndex = i + (tagsToLookFor.Length);
                            break;
                        case 1f:
                            geneIndex = i + (tagsToLookFor.Length * 2);
                            break;
                    }

                    options.Add((geneIndex,
                        dnaGroups._priorityDNA.GetGene(geneIndex) /
                        Vector3.Distance(transform.position, visibleObject.transform.position), pos[index]));
                }
            }
        }
    }


    public (bool, GameObject) TestForTarget(Collider collider, List<GameObject> gameObjects)
    {
        try
        {
            if (collider.attachedRigidbody == GetComponent<Rigidbody>())
            {
                return (false, collider.gameObject);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return (tagsToLookFor.Contains(collider.tag), collider.gameObject);
    }
}
