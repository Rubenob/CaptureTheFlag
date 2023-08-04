using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Flag : NetworkBehaviour
{

    [SerializeField] public bool take;
    [SerializeField] public Player player;
    [SerializeField] public Vector3 spawn;

    private void Start()
    {
        take = false;
        spawn = transform.position;
        player = null;
    }

    void Update()
    {
        if (take)
        {
            transform.position = player.transform.position;
        }
    }

    //Metodo que se llama cuando hay una colision
    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (IsServer)
        {
            //Se comprueba si el jugador tiene ya una bandera para que no pueda llevar dos a la vez
            Player _player = GameManager.GetPlayer(hitInfo.transform.name);
            if (hitInfo.tag == "Player" && !_player.Flag.Value)
            {
                //Si le robas la bandera por contacto a un jugador, lo desvincula de la bandera para que pueda volver a cogerla
                if(this.player != null)
                {
                    this.player.flag = null;
                    this.player.Flag.Value = false;
                }

                //Se asocia la bandera al nuevo jugador que la ha cogido
                take = true;
                this.player = _player;
                this.player.flag = this;
                this.player.Flag.Value = true;
                Debug.Log(transform.name + ": Bandera cogida por " + player.Name.Value);
            }
            //Si la bala choca contra la bandera, la devuelve al spawn
            else if (hitInfo.tag == "Bullet")
            {
                take = false;
                transform.position = spawn;
                if(this.player != null)
                {
                    this.player.flag = null;
                    this.player.Flag.Value = false;
                    player = null;
                }
                Debug.Log(transform.name + ": Bandera dejada");
            }
        }
    }
}
