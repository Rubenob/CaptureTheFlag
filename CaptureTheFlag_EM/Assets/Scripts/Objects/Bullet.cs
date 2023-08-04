using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] Rigidbody2D rb;
    public ulong owner;

    public NetworkVariable<Vector3> Pos;

    void Start()
    {
        Pos = new NetworkVariable<Vector3>();
        Pos.Value = transform.right;

    }

    //Se llama a este metodo cuando Spawnea la bala
    public override void OnNetworkSpawn()
    {
        //Configuras la velocidad de la bala en una direccion
        rb.velocity = transform.right * speed;
    }

    //Metodo que se llama cuando hay una colision
    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (IsServer)
        {
            Debug.Log("[BALA] Impactado contra " + hitInfo.name);
            if (hitInfo.tag == "Player")
            {
                Player player = hitInfo.GetComponent<Player>();
                if (owner != player.OwnerClientId)
                {
                    player.Health.Value--;
                    player.Death();
                    Debug.Log(player.playerName + ": Vida restante - " + player.Health.Value);
                }

            }
            if (hitInfo.tag == "Platform")
            {
                GetComponent<NetworkObject>().Despawn();
            }
        }        
    }

    // Metodo que se utiliza para despawnear la bala desde otra clase
    [ServerRpc]
    public void FarDestroyServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }
}
