# T2.PR1.ThreadsITasks

# Enunciat 1

## Solucio proposada:

1. Evitar deadlocks (interbloquejos)

- Asimetria en l’ordre de bloqueig:

Assigno a cada filòsof un ordre diferent per agafar els palets segons si el seu ID és parell o senar. Això trenca la condició de wait–for cycle (espera circular) i impedeix que es formi un bucle tancat on cada filòsof esperi indefinidament el palet que té un veí.

- Bloqueig mínim:

Utilitzo exclusió mútua (lock) només al voltant de l’aturada crítica (agafar i deixar cada palet), de manera que no mantenim cap bloqueig més temps del necessari.

2. Prevenir gana

- Monitoratge extern:

Un fil secundari (Monitor) comprova cada 500 ms el temps que porta cada filòsof sense menjar.

- Cancel·lació immediata:

Si un filòsof sobrepassa el MaxHungrySeconds, envio una cancel·lació a tots els threads via CancellationTokenSource, la qual atura la simulació i evita fam indefinida.

3. Decisions d’arquitectura i estil

- Ús de CancellationToken per coordinar la finalització neta de tots els threads.

- Registre de missatges amb colors per facilitar la lectura concurrent al terminal.

- Exportació a CSV per analitzar posteriorment el nombre de vegades que han menjat i el temps màxim de fam.

## Esquema de l’estructura del projecte

### Program.cs

Conte el mètode main() i la classe de Philosopher on està el builder i els mètodes necessaris per fer el programa.

## Com has fet per evitar interbloquejos i que ningú passes fam.

Per evitar interbloquejos he introduït una asimetria en l’ordre d’agafar els bastons:

- Els filòsofs amb ID parell agafen primer el palet esquerre i després el dret.

- Els filòsofs amb ID senar fan l’inrevés: primer el palet dret i després l’esquerre.

D’aquesta manera trenco la condició de circular wait (espera cíclica), ja que no tots esperen el mateix recurs en el mateix ordre i, per tant, no es pot formar un bucle tancat de bloqueig.

Per assegurar que cap filòsof passi gana indefinidament:

1. Hi ha un monitor dedicat que, cada 500 ms, comprova quant temps fa que cadascun no menja (LastEatTime).

2. Si detecta que algun filòsof ha estat més de MaxHungrySeconds sense menjar, escriu un missatge d’error i cancel·la tota la simulació.

# Enunciat 2

## Solucio proposada:

- **Separació de responsabilitats**
 
He dividit el joc en tres responsabilitats principals, cadascuna dins d’una tasca (Task o Thread):

1. Lectura d’entrada (teclat): captura ‘A’, ‘D’ i ‘Q’.

2. Lògica física: genera asteroides, mou‐los, detecta col·lisions o esquivades.

3. Renderitzat: neteja la pantalla i dibuixa asteroides i nau a la seva nova posició.

Aquesta separació fa que cada part pugui fer-se de manera continuada i sense bloquejar les altres, millorant la fluïdesa del joc i mantenint el codi modular.

- **Sincronització i seguretat de dades**
  
Tant la llista asteroids com la coordenada shipX són compartides entre les tasques. Per evitar condicions de carrera, envolto qualsevol accés (lectura o escriptura) a aquesta llista amb un lock(lockObj). Així garanteix:

- Que no modifiqui la llista mentre la renderitzo.

- Que no dibuixi un asteroide a mig afegir o eliminar.

- **Controls de freqüència (Hz)**

Enlloc de fer bucles “while” sense descans, he introduït un await Task.Delay(1000/PhysicsHz) i await Task.Delay(1000/RenderHz). D’aquesta manera:

- La simulació física corre a 50 iteracions per segon (50 Hz), suficient per una física suau.

- El renderitzat corre a 20 frames per segon (20 Hz), equilibrant actualització visual i ús de CPU.

- **Cancel·lació cooperativa**
  
Utilitzo un CancellationTokenSource (cts) compartit per les tres tasques. Quan l’usuari prem ‘Q’, fa cts.Cancel(), i cadascuna de les tasques:

- O en surt (en el cas de la d’entrada de teclat).

- O atrapa el TaskCanceledException del Delay i acaba.

- **Persistència de resultats**
  
En finalitzar la partida, gravo un registre CSV amb data/hora, asteroides esquivats, vides perdudes i durada total. Això permet anàlisis posteriors sense alterar la lògica de joc.

## Esquema de l’estructura del projecte

### Program.cs

Lògica principal: Main(), creació de tasques, resum final, CSV

### model/Asteroid.cs 

Classe amb X i Y per a cada asteroide

## Com has executat les tasques per tal de pintar, calcular i escoltar el teclat al mateix temps?

1. ReadInputLoop s’executa en un Task.Run, està pendent de Console.ReadKey(true) i cancel·la via cts.

2. PhysicsLoop funciona amb un bucle que, cada 1/50 s, bloqueja llista i aplica la lògica física abans d’esperar de nou.

3. RenderLoop neteja i dibuixa a la pantalla cada 1/20 s.

Totes tres processen de manera quasi‐simultània gràcies al planificador de tasques de .NET, amb un mecanisme de cancel·lació cooperativa perquè acabin netament quan l’usuari ho demana.

## Has diferenciat entre programació paral·lela i asíncrona?

- **Asíncron:**
  
Utilitzo async/await i Task.Delay per frenar cada bucle sense bloquejar el fil principal i sense consumir cicle de CPU innecessàriament.

- **Paral·lel:**
  
Les tres tasques poden córrer en fils diferents (threads) del pool de .NET de forma paral·lela, aprofitant múltiples nuclis de CPU. És útil per executar la física i el renderitzat alhora, mentre la lectura de teclat espera la volta d’entrada, tot sense bloqueig mutu.

# Bibliografia

BillWagner, GitHubber17, gewarren, EugeneGritsina (13/03/2025) Asynchronous programming with async and await. Learn Microsoft. Recuperat el 9/05/2025 de [Link](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)

IEvangelist, poojapoojari, BillWagner, gewarren, DennisLee-DennisLee, nxtn, mairaw, nemrism, ChrisMaddock, Mikejo5000, guardrex, tompratt-AQ, yishengjin1413 (04/10/2022) Task Parallel Library (TPL). Learn Microsoft. Recuperat el 9/05/2025 de [Link](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl)

ankita_saini (03/02/2025) C# Multithreading. geeksforgeeks. Recuperat el 9/05/2025 de [Link](https://www.geeksforgeeks.org/c-sharp-multithreading/)

Pranaya Rout (17/06/2021) Thread Synchronization Using Lock in C#. dotnettutorials. Recuperat el 9/05/2025 de [Link](https://dotnettutorials.net/lesson/locking-in-multithreading/)

Ravi Karia (1/1/2022) Asynchronous programming with async, await, Task in C#. TutorialsTeacher. Recuperat el 9/05/2025 de [Link](https://www.tutorialsteacher.com/articles/asynchronous-programming-with-async-await-task-csharp)
