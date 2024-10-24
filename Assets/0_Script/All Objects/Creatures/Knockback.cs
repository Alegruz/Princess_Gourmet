using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    public float thrust;
    public float knockTime;
    public float damage;

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayer = gameObject.CompareTag("Player");
        if (other.gameObject.CompareTag("breakable") && isPlayer)
        {
            other.GetComponent<pot>().Smash();
        }
        else if (other.gameObject.CompareTag("enemy") || other.gameObject.CompareTag("Player"))
        {
            Rigidbody2D hit = other.GetComponent<Rigidbody2D>();
            if(hit != null)
            {
                Vector2 difference = hit.transform.position - transform.position;
                difference = difference.normalized * thrust;
                hit.AddForce(difference, ForceMode2D.Impulse);

                if(other.gameObject.CompareTag("enemy") && other.isTrigger)
                {
                    hit.GetComponent<Enemy>().currentState = EnemyState.stagger;
                    other.GetComponent<Enemy>().Knock(hit, knockTime, damage);
                }
                
                if(other.gameObject.CompareTag("Player"))
                {
                    if(other.GetComponent<PlayerManager>().currentState != PlayerState.stagger)
                    {
                        hit.GetComponent<PlayerManager>().currentState = PlayerState.stagger;
                        other.GetComponent<PlayerManager>().Knock(knockTime, damage);
                    }
                }
            }
        }
        else if (isPlayer && other.gameObject.CompareTag("PickUp Object"))
        {
            GetComponentInParent<PlayerManager>().OnKnockback();
        }
    }
}
