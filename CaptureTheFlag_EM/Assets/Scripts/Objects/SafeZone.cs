using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SafeZone : NetworkBehaviour
{
    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        //Compruebas la colision con un jugador 
        if (hitInfo.tag == "Player")
        {
            Player _player = GameManager.GetPlayer(hitInfo.transform.name);
            if (_player.Flag.Value) // Si tiene bandera entra
            {
                //Resetea la bandera y suma punto al player
                _player.Flag.Value = false;
                _player.flag.take = false;
                _player.flag.transform.position = _player.flag.spawn;
                Debug.Log(transform.name + ": Punto para " + _player.transform.name + "!");
                _player.Points.Value += 1;

                //Detecta si los puntos del jugador son igual a 3 y si esto ocurre ese jugador GANA la partida
                if (_player.Points.Value == 3)
                {
                    //Debug.Log("VICTORIA PARA " + _player.transform.name);
                    GameManager.Victory.Value = true;
                }
                //Por ultimo desasocia la bandera al jugador
                _player.flag.player = null;
            }
        }        
    }
}
