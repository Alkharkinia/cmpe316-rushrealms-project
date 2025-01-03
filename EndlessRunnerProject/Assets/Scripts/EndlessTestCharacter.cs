using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;




public class EndlessTestCharacter : MonoBehaviour
{
    public TMP_Text scoreDisplay;

    public float movementSpeed = 10f;   // Speed at which the character moves
    public float jumpForce = 0f;        // Jump force magnitude
    public float forwardSpeed = 5f;     // Constant forward speed (vertical movement)
    public EndlessSpawnManager spawnManager;   // Reference to the SpawnManager

    LevelAudioManager audioManager;

    public int Coins = 0;
    private float startingZ;
    public TextMeshProUGUI distanceTextTMP; // For TextMeshPro UI 
    public TextMeshProUGUI coinText;

    private Transform playerTransform;
    public GameObject enemyPrefab;

    private int playerLayer = 6; // Default player layer
    private int obstacleLayer = 7; // Layer for obstacles
    public GameObject invincibilityGlow;  // Reference to the light object that indicates invincibility
    public GameObject invincibilityObject;

    public TMP_Text countdownText;
    public float invincibilityDuration = 10f;  // Duration for invincibility (in seconds)

    private Rigidbody rb;               // Reference to the Rigidbody component
    private Animator animator;          // Reference to the Animator component

    private bool isGrounded;
    private bool isInvincible = false;

    private float survivalTime = 150f;
    private float gameTime = 0f;
    private bool isGameOver = false;
    private bool hasCollided = false;
    private bool victoryTriggered = false;
    private bool isInputDisabled = false;

    private int phase = 1; // Phase indicator (1 = Phase 1, 2 = Phase 2, 3 = Phase 3)

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<LevelAudioManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();      // Get the Rigidbody component
        animator = GetComponent<Animator>(); // Get the Animator component

        invincibilityGlow.SetActive(false);
        invincibilityObject.SetActive(false);


        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Player GameObject with tag 'Player' not found. Ensure the player has the correct tag.");
        }

        startingZ = playerTransform.position.z;

        PlayerPrefs.SetInt("Score", 0);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {

        if (!isGameOver && !victoryTriggered)
        {
            gameTime += Time.deltaTime;
            //Debug.Log(gameTime);

            // Check if the player survived for the required time
            if (gameTime >= survivalTime)
            {
                StartCoroutine(TriggerVictoryAnimation());
            }
        }

        if (!isGameOver && !victoryTriggered)
        {
            HandleMovement();
            HandleJump();
            HandleStrafing();
            UpdateDistanceUI();
            UpdateScore();
        }

        if (Coins % 50 == 0 && Coins != 0)
        {
            StartCoroutine(Invincibility());
        }
    }

    IEnumerator TriggerVictoryAnimation()
    {
        if (animator != null)
        {
            victoryTriggered = true; // Ensure this happens only once
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            gameObject.GetComponent<EndlessTestCharacter>().forwardSpeed = 0f;
            animator.SetBool("hasWon", true); // Trigger the victory animation
            SpawnEnemies();
        }
        else
        {
            Debug.LogWarning("Player Animator is not assigned!");
        }

        animator.SetBool("enemyHasDied", true);
        yield return new WaitForSeconds(10f);

        if (SceneManager.GetActiveScene().name == "Level01")
        {
            SceneManager.LoadScene("Level01Complete");
        }
        else if (SceneManager.GetActiveScene().name == "Level02")
        {
            SceneManager.LoadScene("Level02Complete");
        }
        else if (SceneManager.GetActiveScene().name == "Level03")
        {
            SceneManager.LoadScene("Level03Complete");
        }
        else if (SceneManager.GetActiveScene().name == "LevelEndless")
        {
            SceneManager.LoadScene("LevelEndlessComplete");
        }
        else
        {
            Debug.LogError("No Valid Scene to Load.");
        }
    }

    void HandleMovement()
    {
        if (!isInputDisabled)
        {

            // Handle horizontal movement (left/right) with player input (A/D or Left/Right Arrow)
            float hMovement = Input.GetAxis("Horizontal") * movementSpeed * Time.deltaTime;

            // Apply constant forward movement along the z-axis (vertical movement in Unity's world space)
            float vMovement = forwardSpeed * Time.deltaTime; // Constant speed on the z-axis

            // Move the character based on input and constant forward speed
            transform.Translate(new Vector3(hMovement, 0, vMovement));

        }
        else
        {
            return;
        }
    }

    void HandleJump()
    {
        // Check for jump input and if the player is grounded
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            // Apply upward force for jumping
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            //Debug.Log("Player Jumped.");

            audioManager.PlaySFX(audioManager.jumpSFX);

            // Set the Animator trigger for the jump animation
            animator.SetTrigger("Jump");

        }

    }

    void HandleStrafing()
    {
        // Right Strafe
        if (Input.GetKeyDown(KeyCode.D)) // Replace 'D' with your desired key
        {

            animator.SetBool("StrafeRight", true);
            animator.SetBool("StrafeLeft", false);
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            animator.SetBool("StrafeRight", false);
        }

        // Left Strafe
        if (Input.GetKeyDown(KeyCode.A)) // Replace 'A' with your desired key
        {

            animator.SetBool("StrafeLeft", true);
            animator.SetBool("StrafeRight", false);
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            animator.SetBool("StrafeLeft", false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Obstacle") && !hasCollided)
        {
            isGameOver = true;
            hasCollided = true;
            isInputDisabled = true;
            gameObject.GetComponent<EndlessTestCharacter>().forwardSpeed = 0f;
            animator.SetBool("Collision", true);
            audioManager.PlaySFX(audioManager.collisionSFX);

            audioManager.PlaySFX(audioManager.playerDeathSFX);

            SpawnEnemies();


        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground")) // Assuming the ground has the "Ground" tag
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    private void SpawnEnemies()
    {
        // Calculate spawn points relative to the player's position
        Vector3 playerPosition = playerTransform.position;
        Vector3 leftSpawnPoint = new Vector3(-1f + playerPosition.x, -3f, -20f + playerPosition.z); // 10 units to the left of the player
        Vector3 rightSpawnPoint = new Vector3(1f + playerPosition.x, -3f, -20f + playerPosition.z); // 10 units to the right of the player

        // Spawn enemies
        GameObject enemyLeft = Instantiate(enemyPrefab, leftSpawnPoint, Quaternion.identity);
        GameObject enemyRight = Instantiate(enemyPrefab, rightSpawnPoint, Quaternion.identity);

        StartCoroutine(MoveEnemy(enemyLeft, playerTransform, -1f)); // Move slightly left of the player
        StartCoroutine(MoveEnemy(enemyRight, playerTransform, 1f));  // Move slightly right of the player
    }

    private IEnumerator MoveEnemy(GameObject enemy, Transform player, float xOffset)
    {
        float duration = 1.5f; // Time to move the enemy
        float elapsed = 0f;
        Vector3 startPosition = enemy.transform.position;

        Vector3 targetPosition = new Vector3(player.position.x + xOffset, enemy.transform.position.y, player.position.z - 2.5f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; // Use unscaled time
            enemy.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            yield return null;
        }

        if (elapsed >= duration && !victoryTriggered)
        {
            // Set the "hasArrived" flag to true to trigger the next animation
            Animator animator = enemy.GetComponent<Animator>(); // Get the Animator component
            if (animator != null)
            {
                audioManager.PlaySFX(audioManager.enemyWinSFX);
                audioManager.PlaySFX(audioManager.enemyWinSFX);
                animator.SetBool("hasArrived", true); // Trigger the animation change
                yield return new WaitForSeconds(2.5f);

                if (SceneManager.GetActiveScene().name == "Level01")
                {
                    SceneManager.LoadScene("GameOverScreen01");
                }
                else if (SceneManager.GetActiveScene().name == "Level02")
                {
                    SceneManager.LoadScene("GameOverScreen02");
                }
                else if (SceneManager.GetActiveScene().name == "Level03")
                {
                    SceneManager.LoadScene("GameOverScreen03");
                }
                else if (SceneManager.GetActiveScene().name == "EndlessLevel")
                {
                    SceneManager.LoadScene("GameOverScreenEndless");
                }
                else
                {
                    Debug.LogError("No Valid Scene to Load.");
                }


            }

        }
        else
        {
            Animator animator = enemy.GetComponent<Animator>();

            animator.SetBool("hasLost", true); // Trigger the animation change
            yield return new WaitForSeconds(8f);



        }

    }

    private void UpdateDistanceUI()
    {
        if (playerTransform != null)
        {
            // Calculate the Z-axis distance from the starting position
            int distance = Mathf.Abs((int)playerTransform.position.z - (int)startingZ);

            if (distanceTextTMP != null)
                distanceTextTMP.text = $"Distance: {distance}m";
        }
    }

    private void OnTriggerEnter(Collider other)
    {


        if (other.CompareTag("SpawnTrigger"))
        {
            spawnManager.SpawnTriggerEntered();

        }

        if (other.transform.tag == "Coin")
        {

            Coins++;
            coinText.text = "Coin: " + Coins.ToString();
            Destroy(other.gameObject);
            audioManager.PlaySFX(audioManager.coinSFX);
        }

    }

    IEnumerator Invincibility()
    {
        isInvincible = true;

        Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, true);

        invincibilityGlow.SetActive(true);
        invincibilityObject.SetActive(true);

        StartCoroutine(InvincibilityCountdown());

        audioManager.PlayLoopingSFX(audioManager.invincibilitySFX);

        // Wait for invincibility duration
        yield return new WaitForSeconds(10f);

        // End invincibility
        isInvincible = false;

        audioManager.StopLoopingSFX();

        Physics.IgnoreLayerCollision(playerLayer, obstacleLayer, false);

        invincibilityGlow.SetActive(false);
    }

    private IEnumerator InvincibilityCountdown()
    {
        float timeRemaining = invincibilityDuration;


        while (timeRemaining > 0)
        {
            // Update the countdown text every frame with an integer value
            countdownText.text = "Invincibility Activated: " + Mathf.RoundToInt(timeRemaining) + "s";
            timeRemaining -= Time.deltaTime;

            yield return null; // Wait until the next frame
        }

        // Once the countdown is over, turn off the light and clear the text
        invincibilityObject.SetActive(false); // Clear the text when invincibility ends
    }

    private void UpdateScore()
    {
        int distance = Mathf.Abs((int)playerTransform.position.z - (int)startingZ);
        int pointsPerMeter = 0;

        // Set points per meter based on the current phase
        switch (phase)
        {
            case 1:
                pointsPerMeter = 2; // Phase 1: 2 points per meter
                break;
            case 2:
                pointsPerMeter = 5; // Phase 2: 5 points per meter
                break;
            case 3:
                pointsPerMeter = 10; // Phase 3: 10 points per meter
                break;
        }

        int score = (distance * pointsPerMeter) + (Coins * 50); // Calculate the score
        PlayerPrefs.SetInt("Score", score); // Save score to PlayerPrefs
        scoreDisplay.text = score.ToString(); // Update score display
    }

}
