using Kepler7.Core;
using System.Numerics;
int assertions = 0;
void Check(bool condition, string label) { if (!condition) throw new Exception("FALLO: " + label); assertions++; Console.WriteLine("OK " + label); }

var original = Prototipos.Ingeniero;
var clon = (Tripulante)original.Clonar();
clon.Habilidades.Add("Ensamblaje de naves"); clon.Uniforme.Color = "Coral"; clon.Nombre = "Misha";
Check(original.Habilidades.Count == 2 && original.Nombre == "Michi-07" && original.Uniforme.Color == "Menta", "Deep clone: original intacto");
Check(!ReferenceEquals(original.Habilidades, clon.Habilidades) && !ReferenceEquals(original.Uniforme, clon.Uniforme), "Colección y objeto anidado independientes");

var taller = new Taller();
foreach (var recipe in RecetaNave.Todas)
{
    Check(taller.Ensamblar(recipe, recipe.Componentes), recipe.Nombre + ": selección correcta");
    Check(taller.UltimaNave!.Componentes.SequenceEqual(recipe.Componentes), recipe.Nombre + ": producto real y orden de Builder");
}
var director = new IngenieroJefe(new NaveBuilder());
var primera = director.Construir(new RecetaCombate());
director.Construir(new RecetaCarga());
Check(!primera.Componentes.Contains(Componente.Tanques), "Reutilizar Builder no modifica el producto previo");
for (int i = 0; i < 3; i++) Check(!taller.Ensamblar(new RecetaCarga(), Array.Empty<Componente>()), "Error de ensamblaje resta un intento");
Check(taller.Vidas == 0 && !taller.Ensamblar(new RecetaCarga(), new RecetaCarga().Componentes), "No se construye sin vidas");
taller.Reiniciar(); Check(taller.Vidas == 3, "Reintentar recupera tres vidas");
Check(!taller.Ensamblar(new RecetaCombate(), Enum.GetValues<Componente>()), "Rechaza componentes extra");

var p = new Progreso { LaboratorioReparado = true };
p.ClonarIngeniero("Jefa", "Menta");
foreach (var recipe in RecetaNave.Todas)
{
    p.ClonarPiloto("Piloto " + recipe.Nombre, "Azul", recipe);
    var nave = director.Construir(recipe); p.RegistrarNave(nave);
    Check(p.AsignarPiloto(nave), "Asignación compatible " + recipe.Nombre);
}
Check(p.IngenieroBase.Habilidades.Count == 2 && p.PilotoBase.Habilidades.Count == 1, "Prototipos reales del laboratorio intactos");
Check(p.Lista && p.PrototypeDominado && p.BuilderDominado, "Flota lista y ambos logros");
Check(!p.AsignarPiloto(p.Flota[0]), "No reasigna nave ocupada");
var mismatch = new Progreso { LaboratorioReparado = true };
mismatch.ClonarIngeniero("Jefa", "Menta"); mismatch.ClonarPiloto("Carga", "Azul", new RecetaCarga());
var combate = director.Construir(new RecetaCombate()); mismatch.RegistrarNave(combate);
Check(!mismatch.AsignarPiloto(combate), "No permite especialidad incompatible");
string file = Path.Combine(Path.GetTempPath(), "kepler-check-" + Guid.NewGuid(), "save.json");
Guardado.Guardar(p, file); var loaded = Guardado.Cargar(file);
Check(loaded.Lista && loaded.Flota.Count == 3 && loaded.Ingeniero!.Nombre == "Jefa", "Guardado y carga de progreso");
Directory.Delete(Path.GetDirectoryName(file)!, true);

var damage = new Pasillo();
damage.Mutantes.Clear(); damage.Mutantes.Add(new Mutante { Posicion = damage.Jugador, Velocidad = 0 });
damage.Tick(.016f, Vector2.Zero, new Vector2(900, 285), false);
Check(damage.Vida == 80, "Contacto resta exactamente 20 de salud");
for (int i = 0; i < 30; i++) damage.Tick(.016f, Vector2.Zero, Vector2.Zero, false);
Check(damage.Vida == 80, "Invulnerabilidad impide daño acumulado inmediato");
for (int i = 0; i < 500; i++) { damage.Mutantes[0].Posicion = damage.Jugador; damage.Tick(.016f, Vector2.Zero, Vector2.Zero, false); }
Check(damage.Derrota && damage.Vida == 0, "Derrota y salud limitada a cero");

var healing = new Pasillo();
healing.Mutantes.Clear(); healing.Mutantes.Add(new Mutante { Posicion = healing.Jugador });
healing.Tick(.016f, Vector2.Zero, Vector2.One, false);
healing.Mutantes.Clear(); healing.Botiquines.Clear(); healing.Botiquines.Add(healing.Jugador);
healing.Tick(.016f, Vector2.Zero, Vector2.One, false);
Check(healing.Vida == 100 && healing.Botiquines.Count == 0, "Botiquín cura sin exceder 100 y se consume");

var fight = new Pasillo();
for (int i = 0; i < 22000 && !fight.Derrota && !fight.PuedeReparar; i++)
{
    var target = fight.Mutantes.OrderBy(m => Vector2.DistanceSquared(m.Posicion, fight.Jugador)).FirstOrDefault();
    float distance = target == null ? 9999 : Vector2.Distance(target.Posicion, fight.Jugador);
    var aim = target?.Posicion ?? new Vector2(2650, 285);
    var move = distance > 500 ? Vector2.UnitX : Vector2.Zero;
    fight.Tick(1f/60f, move, aim, target != null);
}
Check(fight.Bajas == 15 && fight.PuedeReparar && !fight.Derrota, "Recorrido real del pasillo: 15 bajas y acceso a reparación");
// Regresiones de ensamblaje visual y producción sin límite.
var visual = new Taller();
var model = Modelos.Para(0);
var wrongColor = model.Select((part, i) => i == 0 ? part with { Color = "Coral" } : part).ToList();
Check(!visual.EnsamblarModelo(new RecetaExploradora(), model, wrongColor) && visual.Vidas == 2, "Color incorrecto pierde exactamente una vida");
var wrongShape = model.Select((part, i) => i == 0 ? part with { Variante = 1 - part.Variante } : part).ToList();
Check(!visual.EnsamblarModelo(new RecetaExploradora(), model, wrongShape) && visual.Vidas == 1, "Variante incorrecta pierde una vida");
Check(!visual.EnsamblarModelo(new RecetaExploradora(), model, model.Take(3).ToList()) && visual.Vidas == 0, "Pieza faltante agota las tres vidas");
Check(!visual.EnsamblarModelo(new RecetaExploradora(), model, model), "Modelo correcto bloqueado sin vidas");
visual.Reiniciar();
foreach (var ri in Enumerable.Range(0, 3))
{
    var m = Modelos.Para(ri);
    Check(visual.EnsamblarModelo(RecetaNave.Todas[ri], m, m) && visual.UltimaNave!.Piezas.SequenceEqual(m), "Modelo visual persistido " + ri);
}
p.Lanzada = true;
for (int i = 0; i < 15; i++)
{
    visual.EnsamblarModelo(new RecetaExploradora(), model, model);
    p.ClonarPiloto("Extra " + i, "Azul", new RecetaExploradora());
    p.RegistrarNave(visual.UltimaNave!);
    Check(p.AsignarPiloto(visual.UltimaNave!), "Piloto exclusivo para nave repetida " + i);
}
Check(p.Flota.Count == 18 && p.Lista && p.Lanzada, "Producción continúa después de completar y lanzar");
Guardado.Guardar(p, file); loaded = Guardado.Cargar(file);
Check(loaded.Flota.Count == 18 && loaded.Flota.Last().Piezas.SequenceEqual(model), "Guardado conserva flota ampliada, colores y variantes");
var extra = director.Construir(new RecetaExploradora()); loaded.RegistrarNave(extra);
Check(!loaded.AsignarPiloto(extra), "Cargar no libera pilotos ya asignados");
loaded.ClonarPiloto("Nuevo tras cargar", "Coral", new RecetaExploradora());
Check(loaded.AsignarPiloto(extra), "Se puede seguir clonando y asignando tras cargar");
Directory.Delete(Path.GetDirectoryName(file)!, true);
string legacy = Path.Combine(Path.GetTempPath(), "kepler-legacy-" + Guid.NewGuid(), "save.json");
Directory.CreateDirectory(Path.GetDirectoryName(legacy)!);
File.WriteAllText(legacy, "{\"LaboratorioReparado\":true,\"Flota\":[{\"Tipo\":\"Exploradora\",\"Componentes\":[0,1,4,5]}],\"Pilotos\":[]}");
Check(Guardado.Cargar(legacy).Flota[0].Piezas.Count == 0, "Guardados anteriores sin piezas visuales siguen cargando");
Directory.Delete(Path.GetDirectoryName(legacy)!, true);
var repair = new ReparacionClonadora();
Check(!repair.Conectar(0, 0) && !repair.Completa, "No conecta cables antes de fijar módulos");
Check(!repair.Colocar(0, 1) && !repair.Modulos[0], "Pieza no entra en hueco incompatible");
repair.Atornillar(0, 1);
Check(repair.Tornillos[0] == 0, "No atornilla un módulo ausente");
Check(repair.Colocar(0, 0) && repair.Colocar(1, 1), "Ambas piezas encastran");
Check(!repair.Colocar(0, 0), "No instala dos veces la misma pieza");
repair.Atornillar(0, MathF.PI);
Check(repair.Tornillos[0] == 0, "Saltos de mouse no cuentan como vueltas");
repair.Atornillar(0, .5f); repair.Atornillar(0, -.5f);
Check(repair.Tornillos[0] == 0, "Girar al revés deshace ajuste");
for (int screw = 0; screw < 4; screw++)
    for (int turn = 0; turn < 40; turn++) repair.Atornillar(screw, .3f);
Check(repair.Fijada && !repair.Completa, "Cuatro tornillos firmes habilitan cableado");
Check(!repair.Conectar(0, 2) && !repair.Cables[0], "Cable equivocado no se fija");
for (int cable = 0; cable < 4; cable++) Check(repair.Conectar(cable, cable), "Conecta pareja correcta " + cable);
Check(repair.Completa && repair.Avance == 1 && !repair.Conectar(0, 0), "Reparación completa sin repetir conexiones");
// Flyweight + Prototype: comparte lo intrínseco, copia lo extrínseco.
var catalog = new BibliotecaTripulantes();
Check(catalog.ConFlyweight.Count == 10000 && catalog.SinFlyweight.Count == 10000, "Comparación real con 10.000 individuos por implementación");
Check(catalog.Fabrica.Cantidad == 5 && catalog.Fabrica.ObjetosCreados == 5 && catalog.ObjetosSinCompartir == 10000, "Cinco especies compartidas frente a 10.000 sin Flyweight");
Check(ReferenceEquals(catalog.ConFlyweight[0].Especie,catalog.ConFlyweight[5].Especie) && !ReferenceEquals(catalog.SinFlyweight[0].Especie,catalog.SinFlyweight[5].Especie), "Reutilización real por referencia");
var prototype = Prototipos.ConAspecto(Prototipos.Piloto,"Calicó");
var copy = (Tripulante)prototype.Clonar(); copy.Uniforme.Color="Coral";copy.Habilidades.Add("Prueba");copy.Aspecto.Marcas.Add("Marca propia");copy.Rango="Capitán";
Check(ReferenceEquals(copy.Especie,prototype.Especie) && copy.Identificador != prototype.Identificador, "Clon comparte IEspecie y recibe identificador nuevo");
Check(prototype.Uniforme.Color=="Menta" && prototype.Aspecto.Marcas.Count==1 && prototype.Habilidades.Count==1 && prototype.Rango=="Cadete", "Uniforme, marcas, habilidades y rango independientes");
var batchProgress = new Progreso { LaboratorioReparado=true };batchProgress.ClonarIngeniero("Jefa","Dorado","Negro");
var batch=batchProgress.ClonarPilotos(8,"Luna","Azul",new RecetaCarga(),"Siamés");
Check(batch.Count==8 && batch.Select(x=>x.Identificador).Distinct().Count()==8 && batch.Select(x=>x.Nombre).Distinct().Count()==8, "Lote con ocho identidades únicas");
Check(batch.All(x=>ReferenceEquals(x.Especie,batch[0].Especie)) && !ReferenceEquals(batch[0].Uniforme,batch[1].Uniforme), "Lote comparte especie y conserva uniformes separados");
batch[0].Uniforme.Color="Coral";Check(batch[1].Uniforme.Color=="Azul" && batchProgress.PilotoBase.Uniforme.Color=="Menta", "Recolorar un clon no cambia al hermano ni al original");
batchProgress.ClonarPilotos(8,"Luna","Azul",new RecetaCarga(),"Siamés");
Check(batchProgress.Pilotos.Select(x=>x.Nombre).Distinct().Count()==16,"Lotes repetidos no duplican nombres");
bool invalidBatch=false;try{batchProgress.ClonarPilotos(0,"X","Azul",new RecetaCarga(),"Negro");}catch(ArgumentOutOfRangeException){invalidBatch=true;}
Check(invalidBatch && batchProgress.Pilotos.Count==16,"Lote inválido no cambia la población");
var assigned=director.Construir(new RecetaCarga());batchProgress.RegistrarNave(assigned);
var wrong=batchProgress.ClonarPiloto("Combate","Menta",new RecetaCombate(),"Blanco");
Check(!batchProgress.AsignarPiloto(assigned,wrong) && assigned.Piloto==null,"Abordaje rechaza selección incompatible sin asignar");
Check(batchProgress.AsignarPiloto(assigned,batch[0]) && ReferenceEquals(assigned.Piloto,batch[0]),"Abordaje asigna exactamente al piloto seleccionado");
var another=director.Construir(new RecetaCarga());batchProgress.RegistrarNave(another);
Check(!batchProgress.AsignarPiloto(another,batch[0]),"Un piloto no puede abordar dos naves");
Guardado.Guardar(batchProgress,file);var batchLoaded=Guardado.Cargar(file);
Check(batchLoaded.Pilotos[0].Uniforme.Color=="Coral" && batchLoaded.Pilotos[0].Aspecto.Tipo=="Siamés" && batchLoaded.Pilotos[0].Identificador==batch[0].Identificador,"Guardado preserva pelaje, remera e identificador");
Check(ReferenceEquals(batchLoaded.Pilotos[0].Especie,batchLoaded.Pilotos[1].Especie) && !batchLoaded.AsignarPiloto(batchLoaded.Flota[1],batchLoaded.Pilotos[0]),"Carga reconstruye Flyweight y conserva ocupación");
Directory.Delete(Path.GetDirectoryName(file)!,true);
var rng=new Random(812);var designs=new HashSet<string>();
for(int recipe=0;recipe<3;recipe++)
{
    List<Pieza>? previousModel=null;
    for(int j=0;j<50;j++)
    {
        var design=Modelos.Aleatorio(recipe,rng,previousModel);
        if(!design.Select(x=>x.Componente).SequenceEqual(RecetaNave.Todas[recipe].Componentes) || design.Select(x=>x.Color).Distinct().Count()<2 || design.Any(x=>x.Variante<0 || x.Variante>1))throw new Exception("Modelo aleatorio inválido");
        if(previousModel!=null && design.SequenceEqual(previousModel))throw new Exception("Modelo repetido");
        designs.Add(string.Join(";",design));previousModel=design;
    }
}
Check(designs.Count>120,"150 modelos aleatorios variados, multicolor y con componentes de su receta");
var repeated=Modelos.Aleatorio(0,new Random(13));var avoided=Modelos.Aleatorio(0,new Random(13),repeated);
Check(!repeated.SequenceEqual(avoided),"Se evita repetir incluso con la misma secuencia aleatoria");
// Tanques sólidos y censo persistente.
var tanks=new Pasillo();
Check(tanks.Capsulas.Length==6 && tanks.Mutantes.All(m=>!tanks.Solidos.Any(o=>o.Toca(m.Posicion,20))),"Ningún zombie nace dentro de tanques ni muebles");
tanks.Mutantes.Clear();
for(int step=0;step<31;step++)tanks.Tick(1f/60,Vector2.UnitX,Vector2.Zero,false);
for(int step=0;step<90;step++)tanks.Tick(1f/60,-Vector2.UnitY,Vector2.Zero,false);
Check(tanks.Jugador.Y>=tanks.Capsulas[0].Y+tanks.Capsulas[0].H+21 && tanks.Jugador.Y<265,"Tanque bloquea al jugador desde abajo sin caja invisible excesiva");
var shot=new Disparo { Posicion=new Vector2(240,270),Velocidad=new Vector2(0,-780) };tanks.Disparos.Add(shot);
for(int step=0;step<12;step++)tanks.Tick(1f/60,Vector2.Zero,Vector2.Zero,false);
Check(!tanks.Disparos.Contains(shot),"El tanque detiene proyectiles");
for(int step=0;step<40;step++)tanks.Tick(1f/60,-Vector2.UnitX,Vector2.Zero,false);
for(int step=0;step<35;step++)tanks.Tick(1f/60,-Vector2.UnitY,Vector2.Zero,false);
for(int step=0;step<85;step++)tanks.Tick(1f/60,Vector2.UnitX,Vector2.Zero,false);
Check(tanks.Jugador.X<=tanks.Capsulas[0].X-21 && !tanks.Solidos.Any(o=>o.Toca(tanks.Jugador,21)),"El jugador rodea el tanque y también colisiona por el lateral");
var chasing=new Pasillo();bool overlaps=false;
for(int step=0;step<1200 && !chasing.Derrota;step++){chasing.Tick(1f/60,Vector2.Zero,new Vector2(2600,285),true);overlaps|=chasing.Mutantes.Any(m=>chasing.Solidos.Any(o=>o.Toca(m.Posicion,20)));}
Check(!overlaps,"Zombies en movimiento respetan tanques y muebles");
var emptyCensus=new CensoPoblacion(new Progreso());
Check(emptyCensus.Total==10000 && emptyCensus.Naves==0,"Censo inicial incluye los 10.000 habitantes sin naves");
var census=new CensoPoblacion(batchLoaded);
Check(census.Total==10018 && census.Pilotos==17 && census.Asignados==1 && census.Disponibles==16,"Censo cuenta cada clon una vez después de guardar y cargar");
Check(census.Naves==2 && census.NavesSinPiloto==1 && census.Registros.Single(r=>r.NumeroNave.HasValue).Tripulante.Identificador==batch[0].Identificador,"Censo vincula la asignación a la nave y al piloto correcto");
Check(census.Registros.Count(r=>r.Tripulante.Especialidad=="Carga")==16 && census.Registros.Count(r=>r.Tripulante.Especialidad=="Combate")==1,"Censo conserva especialidades de los lotes de clones");
batchLoaded.ClonarPilotos(20,"Nuevo","Menta",new RecetaExploradora(),"Blanco");
Check(new CensoPoblacion(batchLoaded).Total==10038,"Reabrir censo refleja nuevos lotes sin límite de páginas");
Check(new Pasillo().Mutantes.Select(m=>m.VarianteVisual).Distinct().Count()==4,"Cuatro variantes visuales estables de zombies");
var layout=new Pasillo();
Check(layout.Obstaculos.All(a=>layout.Capsulas.All(b=>a.X+a.W+20<=b.X || b.X+b.W+20<=a.X || a.Y+a.H+20<=b.Y || b.Y+b.H+20<=a.Y)),"Muebles y tanques separados por al menos 20 unidades, sin superposición visual");
batchLoaded.GuiasVistas.Add("combate");Guardado.Guardar(batchLoaded,file);var withGuides=Guardado.Cargar(file);
Check(withGuides.GuiasVistas.Contains("combate") && !withGuides.GuiasVistas.Contains("ensamblaje"),"Ayudas contextuales vistas se guardan por separado para cada sector");
File.WriteAllText(file,"{\"Pilotos\":[],\"Flota\":[]}");
Check(Guardado.Cargar(file).GuiasVistas.Count==0,"Guardados anteriores sin ayudas siguen siendo compatibles");
Directory.Delete(Path.GetDirectoryName(file)!,true);
var largeBatch=new Progreso{LaboratorioReparado=true};largeBatch.ClonarIngeniero("Jefa","Menta");
foreach(int n in new[]{1,10,30,100})Check(largeBatch.ClonarPilotos(n,"Lote","Azul",new RecetaCarga(),"Blanco").Count==n,"Clonación de "+n+" individuos");
Check(largeBatch.Pilotos.Count==141 && largeBatch.Pilotos.Select(t=>t.Nombre).Distinct().Count()==141,"Lotes grandes conservan cantidad e identidades únicas");
var emergency=new Emergencia();emergency.Avanzar(40,true);
Check(emergency.Restante==120,"Leer instrucciones o pausar no consume tiempo");
emergency.Avanzar(60);Check(emergency.Restante==60 && !emergency.Agotada,"Cuenta regresiva compartida de dos minutos");
emergency.Resolver();emergency.Avanzar(100);Check(emergency.Resuelta && emergency.Restante==60 && !emergency.Agotada,"Reparar a tiempo detiene la emergencia");
emergency.Reiniciar();emergency.Avanzar(121);Check(emergency.Agotada && emergency.Restante==0,"Tiempo agotado termina la misión sin valores negativos");
emergency.Resolver();Check(!emergency.Resuelta,"No se puede resolver una emergencia vencida");
emergency.Reiniciar();Check(emergency.Restante==120 && !emergency.Resuelta && !emergency.Agotada,"Reintentar restaura los dos minutos");
emergency.Avanzar(-2);emergency.Avanzar(float.NaN);Check(emergency.Restante==120,"Tiempo inválido no altera el reloj");
Check(MedallaRescate.Obtener(60).Nivel=="Oro","Oro hasta exactamente un minuto");
Check(MedallaRescate.Obtener(59.9f).Nivel=="Plata" && MedallaRescate.Obtener(30).Nivel=="Plata","Plata después de un minuto y hasta 1:30");
Check(MedallaRescate.Obtener(29.9f).Nivel=="Bronce" && MedallaRescate.Obtener(.1f).Nivel=="Bronce","Bronce después de 1:30 y antes del vencimiento");
bool expiredMedal=false;try{MedallaRescate.Obtener(0);}catch(ArgumentOutOfRangeException){expiredMedal=true;}Check(expiredMedal,"Una misión vencida no recibe medalla");
largeBatch.MedallaRescate=MedallaRescate.Obtener(80);Guardado.Guardar(largeBatch,file);var awarded=Guardado.Cargar(file);
Check(awarded.MedallaRescate?.Nivel=="Oro" && awarded.MedallaRescate.TiempoSegundos==40,"Guardado conserva medalla y tiempo del rescate");
Directory.Delete(Path.GetDirectoryName(file)!,true);
var alarm=new Emergencia();alarm.Avanzar(105);Check(!alarm.Critica,"La alarma no empieza antes de bajar de 15 segundos");
alarm.Avanzar(.1f);Check(alarm.Critica,"Alarma crítica al bajar de 15 segundos");
alarm.Resolver();Check(!alarm.Critica,"La victoria apaga el estado crítico");
alarm.Reiniciar();alarm.Avanzar(121);Check(!alarm.Critica && alarm.Agotada,"El vencimiento detiene el estado de alarma");
var bulkProgress=new Progreso{LaboratorioReparado=true};bulkProgress.ClonarIngeniero("Jefa lotes","Menta");
var bulkWorkshop=new Taller();
foreach(var r in RecetaNave.Todas)
{
    var lotModel=Modelos.Aleatorio(Array.FindIndex(RecetaNave.Todas,x=>x.Nombre==r.Nombre),new Random(7));
    var ships=bulkWorkshop.EnsamblarLote(r,lotModel,lotModel,10);
    Check(ships.Count==10 && ships.All(n=>n.Tipo==r.Nombre && n.Piezas.SequenceEqual(lotModel)),"Lote válido de 10 naves "+r.Nombre);
    Check(!ReferenceEquals(ships[0],ships[1]) && !ReferenceEquals(ships[0].Piezas,ships[1].Piezas) && !ReferenceEquals(ships[0].Componentes,ships[1].Componentes),"Naves y colecciones independientes en "+r.Nombre);
    foreach(var n in ships)bulkProgress.RegistrarNave(n);
    bulkProgress.ClonarPilotos(7,"Lote "+r.Nombre,"Azul",r,"Calicó");
}
var failedBulk=bulkWorkshop.EnsamblarLote(new RecetaCarga(),Modelos.Para(2),Array.Empty<Pieza>(),100);
Check(failedBulk.Count==0 && bulkWorkshop.Vidas==2,"Lote fallido no crea naves y resta sólo una vida");
var filteredAssignments=bulkProgress.AsignarLote(3,"Carga");
Check(filteredAssignments.Count==3 && filteredAssignments.All(n=>n.Tipo=="Carga" && n.Piloto?.Especialidad=="Carga"),"Asignación en lote respeta especialidad y cantidad");
var allAssignments=bulkProgress.AsignarLote(100);
Check(allAssignments.Count==18 && bulkProgress.Flota.Count(n=>n.Piloto==null)==9,"Lote parcial asigna sólo parejas disponibles");
Check(bulkProgress.Flota.Where(n=>n.Piloto!=null).Select(n=>n.Piloto!.Identificador).Distinct().Count()==21 && bulkProgress.Flota.All(n=>n.Piloto==null || n.Piloto.Especialidad==n.Tipo),"Ningún piloto duplicado ni especialidad incorrecta en lotes mixtos");
Check(bulkProgress.AsignarLote(10).Count==0,"Repetir lote no reutiliza pilotos ocupados");
bulkProgress.ClonarPilotos(10,"Refuerzo","Coral",new RecetaCarga(),"Negro");
Check(bulkProgress.AsignarLote(100,"Carga").Count==3,"Nuevos pilotos cubren sólo naves pendientes del filtro");
var bulkCensus=new CensoPoblacion(bulkProgress);
Check(bulkCensus.Filtrar("Todos").Total==10032,"Todos incluye habitantes, ingeniero y pilotos");
var cargoCensus=bulkCensus.Filtrar("Carga");
Check(cargoCensus.Total==17 && cargoCensus.Asignados==10 && cargoCensus.Disponibles==7 && cargoCensus.Naves==10 && cargoCensus.NavesSinPiloto==0,"Censo Carga filtra listas, contadores y naves juntos");
Check(bulkCensus.Filtrar("Disponibles").Total==7 && bulkCensus.Filtrar("Disponibles").Asignados==0 && bulkCensus.Filtrar("Disponibles").Naves==6,"Filtro disponibles excluye piloto a bordo e ingeniero");
Check(new CensoPoblacion(new Progreso()).Filtrar("Combate").Total==0,"Filtro vacío devuelve cero sin errores");
Guardado.Guardar(bulkProgress,file);var bulkReload=Guardado.Cargar(file);
Check(bulkReload.Flota.Count==30 && bulkReload.Flota.Count(n=>n.Piloto!=null)==24 && bulkReload.AsignarLote(100).Count==0,"Guardado conserva lotes e impide reasignaciones duplicadas");
Directory.Delete(Path.GetDirectoryName(file)!,true);
bool invalidLot=false;try{bulkWorkshop.EnsamblarLote(new RecetaCarga(),Modelos.Para(2),Modelos.Para(2),0);}catch(ArgumentOutOfRangeException){invalidLot=true;}
Check(invalidLot && bulkWorkshop.Vidas==2,"Cantidad inválida no consume vidas");
var support=bulkCensus.Filtrar("Tripulación");
Check(support.Total==1 && support.Registros.Count(r=>r.Tripulante.Rol=="Ingeniero")==1 && support.Disponibles==0 && support.Asignados==0,"Tripulación agrupa al ingeniero sin habitantes ni pilotos");
Check(new CensoPoblacion(new Progreso()).Filtrar("Población").Total==10000,"Población fundacional existe antes de clonar al ingeniero");
Check(bulkCensus.Filtrar("Población").Total==10000 && bulkCensus.Filtrar("Población").Registros.All(r=>r.Tripulante.Rol=="Habitante"),"Población contiene sólo los 10.000 habitantes");
Check(new CensoPoblacion(new Progreso()).Filtrar("Tripulación").Total==0,"Tripulación vacía antes de crear al ingeniero");
Check(bulkCensus.Filtrar("Todos").Total==bulkCensus.Filtrar("Población").Total+support.Total+bulkCensus.Pilotos,"Categorías suman el total sin duplicados");
var registered=new Progreso();int originalXp=registered.XP;
Check(registered.RegistrarCenso() && !registered.RegistrarCenso() && registered.XP==originalXp+100,"Insignia Flyweight otorga XP una sola vez");
var realLibrary=new BibliotecaTripulantes(registered.Habitantes);
Check(ReferenceEquals(realLibrary.ConFlyweight,registered.Habitantes) && realLibrary.Fabrica.Cantidad==5 && realLibrary.ObjetosSinCompartir==10000,"Comparación usa los habitantes reales con sólo cinco especies");
Guardado.Guardar(registered,file);var censusReload=Guardado.Cargar(file);
Check(censusReload.FlyweightDominado && !censusReload.RegistrarCenso() && censusReload.Habitantes.Select(t=>t.Identificador).SequenceEqual(registered.Habitantes.Select(t=>t.Identificador)),"Guardado conserva insignia, XP e identidades fundacionales");
Check(censusReload.Pilotos.Count==0 && censusReload.Habitantes.All(t=>t.Rol!="Piloto"),"Habitantes no se convierten en pilotos disponibles");
Directory.Delete(Path.GetDirectoryName(file)!,true);
var dynamicLibrary=BibliotecaTripulantes.DesdeProgreso(bulkProgress);
Check(dynamicLibrary.ConFlyweight.Count==10032 && dynamicLibrary.ObjetosSinCompartir==10032 && dynamicLibrary.ObjetosCompartidos==5,"Flyweight incluye habitantes, ingeniero y pilotos sin duplicar asignados");
Check(Math.Abs(dynamicLibrary.ReduccionPorcentaje-100d*(1-5d/10032))<.0000001,"Porcentaje Flyweight calculado sobre población actual");
bulkProgress.ClonarPiloto("Nuevo censo","Coral",new RecetaCarga());
Check(BibliotecaTripulantes.DesdeProgreso(bulkProgress).ObjetosSinCompartir==10033,"Reabrir comparación refleja nuevos clones");
// Bridge: nueve combinaciones reales y cambio de implementación en la misma abstracción.
ICifrado[] cipherChecks={new SinCifrado(),new CifradoAES(),new CifradoCuantico()};
foreach(var cipher in cipherChecks)
foreach(int type in Enumerable.Range(0,3))
{
    Mensaje message=type switch{0=>new MensajeTexto(cipher),1=>new MensajeAlerta(cipher),_=>new MensajeEmergencia(cipher)};
    var sent=message.Enviar("¡Michi ñ! Kepler-7 solicita ayuda.");
    Check(sent.Recibido==sent.Original && sent.Tipo==message.Tipo && sent.Cifrado==cipher.Nombre,"Bridge ida y vuelta: "+message.Tipo+" / "+cipher.Nombre);
}
var mutableMessage=new MensajeAlerta(cipherChecks[0]);var plain=mutableMessage.Enviar("Prueba");mutableMessage.Cifrado=cipherChecks[1];var encrypted=mutableMessage.Enviar("Prueba");
Check(plain.Resultado==plain.Original && encrypted.Resultado!=encrypted.Original && plain.Original==encrypted.Recibido,"Bridge cambia cifrado sin cambiar tipo de mensaje");
Check(encrypted.Resultado!=mutableMessage.Enviar("Prueba").Resultado,"AES genera IV nuevo por transmisión");
bool blankRejected=false;try{mutableMessage.Enviar(" ");}catch(ArgumentException){blankRejected=true;}Check(blankRejected,"Mensaje vacío no se transmite");
var link=new EnlaceComunicaciones(new Random(5));bool notReady=false;
try{link.Transmitir(mutableMessage,"Ayuda");}catch(InvalidOperationException){notReady=true;}Check(notReady && !link.Listo,"Antenas desalineadas bloquean el envío");
for(int channel=0;channel<3;channel++)link.Ajustar(channel,link.Objetivos[channel]);
link.Avanzar(.6f);Check(link.Alineado && !link.Listo,"Se exige estabilización y no sólo tocar la marca");
link.Avanzar(.6f);Check(link.Listo && link.Transmitir(mutableMessage,"Ayuda").Recibido=="[Alerta] Ayuda","Alineación estable habilita transmisión real");
link.Ajustar(0,0);Check(!link.Listo,"Mover una antena fuera de rango pierde estabilidad");
link.Ajustar(0,float.NaN);link.Avanzar(float.NaN);Check(float.IsFinite(link.Ajustes[0]) && float.IsFinite(link.Estabilidad),"Entrada inválida no rompe las antenas");
var bridgeProgress=new Progreso();
Check(!bridgeProgress.RegistrarTransmision(new MensajeTexto(cipherChecks[0]).Enviar("Uno")),"Una transmisión no concede Bridge prematuramente");
Check(!bridgeProgress.RegistrarTransmision(new MensajeAlerta(cipherChecks[1]).Enviar("Dos")),"Dos tipos todavía no completan Bridge");
Check(bridgeProgress.RegistrarTransmision(new MensajeEmergencia(cipherChecks[2]).Enviar("Tres")) && bridgeProgress.XP==150,"Tres tipos y cifrados desbloquean Bridge y 150 XP");
Check(!bridgeProgress.RegistrarTransmision(new MensajeTexto(cipherChecks[0]).Enviar("Otra")) && bridgeProgress.XP==150,"Insignia Bridge no repite XP");
for(int n=0;n<22;n++)bridgeProgress.RegistrarOperacion(new ResumenOperacion{Titulo="Lote "+n,Detalles=new(){"Resultado "+n}});
Check(bridgeProgress.Operaciones.Count==20 && bridgeProgress.Operaciones[0].Titulo=="Lote 2","Bitácora conserva las últimas 20 operaciones");
Guardado.Guardar(bridgeProgress,file);var bridgeReload=Guardado.Cargar(file);
Check(bridgeReload.BridgeDominado && bridgeReload.Transmisiones.Count==4 && bridgeReload.Transmisiones[2].Recibido=="[Emergencia] Tres" && bridgeReload.Operaciones.Count==20,"Guardado conserva historial, resultados, lotes e insignia Bridge");
Directory.Delete(Path.GetDirectoryName(file)!,true);
var lookup=new CensoPoblacion(bulkProgress);
Check(lookup.Filtrar("Población","k7-10000").Total==1,"Buscador encuentra un habitante por ID");
Check(lookup.Filtrar("Población","michi 10000").Total==1,"Buscador ignora mayúsculas");
Check(lookup.Filtrar("Población","","Siamés","Habitante").Total==2000,"Filtros combinan especie y rol");
Check(lookup.Filtrar("Tripulación","","Todas","Piloto").Total==0,"Filtros incompatibles devuelven vacío sin mezclar grupos");
Check(lookup.Filtrar("Todos","","Todas","Piloto","A bordo").Total==24,"Filtro a bordo excluye habitantes y pilotos libres");
Check(lookup.Filtrar("Todos","","Todas","Piloto","Libres").Total==8,"Filtro libres coincide con asignaciones actuales");
var accented=bulkProgress.ClonarPiloto("Mísha única","Coral",new RecetaCarga());
Check(new CensoPoblacion(bulkProgress).Filtrar("Todos","misha unica").Total==1,"Búsqueda tolera acentos");
var counts=EstadisticasFlota.Obtener(bulkProgress);
Check(counts.Sum(r=>r.PilotosDisponibles)==9 && counts.Sum(r=>r.NavesSinPiloto)==6,"Indicadores de habitación coinciden con la flota");
Check(Logros.Obtener(bridgeReload).Single(l=>l.Patron=="Bridge").Obtenida && !Logros.Obtener(new Progreso()).Any(l=>l.Obtenida),"Panel de logros refleja progreso real y partida nueva");
var currentCensus=new CensoPoblacion(bulkProgress);
Check(currentCensus.Filtrar("Habitantes").Total==10000,"Botón Habitantes conserva sólo población fundacional");
Check(currentCensus.Filtrar("Tripulantes").Registros.All(r=>r.Tripulante.Rol!="Piloto" && r.Tripulante.Rol!="Habitante"),"Botón Tripulantes excluye pilotos y habitantes");
var beforeClones=BibliotecaTripulantes.DesdeProgreso(bulkProgress);
int speciesBefore=FabricaEspecies.Compartida.ObjetosCreados;
var extraCats=bulkProgress.ClonarPilotos(10,"Nueva carga","Coral",new RecetaCarga(),"Atigrado");
var afterClones=BibliotecaTripulantes.DesdeProgreso(bulkProgress);
Check(afterClones.ConFlyweight.Count==beforeClones.ConFlyweight.Count+10 && afterClones.ObjetosSinCompartir==beforeClones.ObjetosSinCompartir+10,"Diez clones aumentan personas y comparación sin Flyweight en diez");
Check(afterClones.ObjetosCompartidos==5 && FabricaEspecies.Compartida.ObjetosCreados==speciesBefore && extraCats.All(t=>ReferenceEquals(t.Especie,FabricaEspecies.Compartida.Obtener("Atigrado"))),"Los nuevos clones reutilizan referencias sin crear más especies compartidas");
Check(new CensoPoblacion(bulkProgress).Filtrar("Carga","Nueva carga","Atigrado").Total==10,"Especialidad combina nombre y especie con clones nuevos");
var storyProgress=new Progreso();
Check(ComunicacionesHistoria.Siguiente(storyProgress)==null,"Historia no informa rescate antes de reparar");
storyProgress.LaboratorioReparado=true;
var report=ComunicacionesHistoria.Siguiente(storyProgress)!;
Check(report.Id=="rescate" && report.Tipo==2,"Primer informe confirma contención de la emergencia");
Check(storyProgress.RegistrarTransmision(new Transmision{MisionId=report.Id,RespuestaTierra=report.Respuesta}) && storyProgress.BridgeDominado,"Primer informe de misión restablece contacto y otorga insignia Bridge");
Check(ComunicacionesHistoria.Siguiente(storyProgress)==null && ComunicacionesHistoria.ProximoObjetivo(storyProgress).Contains("Ingeniero"),"Informe enviado espera el siguiente hito");
storyProgress.ClonarIngeniero("Jefa historia","Menta");
Check(ComunicacionesHistoria.Siguiente(storyProgress)?.Id=="ingeniero","Ingeniero desbloquea su informe");
storyProgress.RegistrarTransmision(new Transmision{MisionId="ingeniero"});
var firstStoryShip=new IngenieroJefe(new NaveBuilder()).Construir(new RecetaExploradora());storyProgress.RegistrarNave(firstStoryShip);
Check(ComunicacionesHistoria.Siguiente(storyProgress)?.Id=="primera-nave","Primera nave desbloquea aviso de construcción");
Check(!storyProgress.RegistrarTransmision(new Transmision{MisionId="primera-nave"}) && storyProgress.BridgeDominado,"Informes posteriores conservan insignia sin repetir premio");
int storyCount=storyProgress.Transmisiones.Count,storyXP=storyProgress.XP;
Check(!storyProgress.RegistrarTransmision(new Transmision{MisionId="primera-nave"}) && storyProgress.Transmisiones.Count==storyCount && storyProgress.XP==storyXP,"Informe duplicado no se registra ni repite premio");
Check(ComunicacionesHistoria.Siguiente(storyProgress)==null,"No anuncia flota lista sin tres pilotos compatibles");
foreach(var recipe in RecetaNave.Todas){var ship=storyProgress.Flota.FirstOrDefault(n=>n.Tipo==recipe.Nombre);if(ship==null){ship=new IngenieroJefe(new NaveBuilder()).Construir(recipe);storyProgress.RegistrarNave(ship);}var pilot=storyProgress.ClonarPiloto("Historia "+recipe.Nombre,"Azul",recipe);storyProgress.AsignarPiloto(ship,pilot);}
Check(ComunicacionesHistoria.Siguiente(storyProgress)?.Id=="flota-lista","Tres tipos preparados desbloquean confirmación de flota");
storyProgress.RegistrarTransmision(new Transmision{MisionId="flota-lista"});storyProgress.Lanzada=true;
Check(ComunicacionesHistoria.Siguiente(storyProgress)?.Id=="despegue","Lanzamiento desbloquea confirmación de despegue");
storyProgress.RegistrarTransmision(new Transmision{MisionId="despegue"});
Check(ComunicacionesHistoria.Siguiente(storyProgress)==null,"Los cinco informes se completan sin repetir mensajes");
var lateArrival=new Progreso{LaboratorioReparado=true,Lanzada=true};
Check(ComunicacionesHistoria.Siguiente(lateArrival)?.Id=="despegue","Partida avanzada informa su etapa actual sin pedir tareas ya completadas");
lateArrival.RegistrarTransmision(new Transmision{MisionId="despegue"});
Check(ComunicacionesHistoria.Siguiente(lateArrival)==null,"No se ofrecen informes atrasados tras confirmar el despegue");
var storyFile=Path.Combine(Path.GetTempPath(),"kepler-story-"+Guid.NewGuid(),"save.json");Guardado.Guardar(storyProgress,storyFile);var storyReload=Guardado.Cargar(storyFile);
Check(storyReload.Transmisiones[0].RespuestaTierra==report.Respuesta && ComunicacionesHistoria.Siguiente(storyReload)==null,"Guardado conserva informes y respuestas sin habilitar duplicados");
Directory.Delete(Path.GetDirectoryName(storyFile)!,true);
var boarding30=new Progreso{LaboratorioReparado=true};boarding30.ClonarIngeniero("Ingeniera lotes","Menta");
var combat30=boarding30.ClonarPilotos(30,"Combate lote","Coral",new RecetaCombate(),"Siamés");
boarding30.ClonarPilotos(30,"Exploración lote","Azul",new RecetaExploradora(),"Atigrado");
for(int i=0;i<30;i++){boarding30.RegistrarNave(new IngenieroJefe(new NaveBuilder()).Construir(new RecetaExploradora()));boarding30.RegistrarNave(new IngenieroJefe(new NaveBuilder()).Construir(new RecetaCombate()));}
Check(!boarding30.AsignarPiloto(boarding30.Flota[0],combat30[0]) && boarding30.Flota[0].Piloto==null && !boarding30.PilotoOcupado(combat30[0]),"Intento incorrecto deja nave vacía y piloto libre");
Check(boarding30.PilotosDisponibles("Combate").Length==30,"Piloto rechazado sigue disponible para su especialidad");
var assigned30=boarding30.AsignarLote(30,"Combate","Siamés");
Check(assigned30.Count==30 && assigned30.All(n=>n.Tipo=="Combate" && n.Piloto!.Especialidad=="Combate") && assigned30.Select(n=>n.Piloto!.Identificador).Distinct().Count()==30,"Treinta pilotos compatibles ocupan treinta naves distintas");
var combatStats=EstadisticasFlota.Obtener(boarding30).Single(s=>s.Tipo=="Combate");
Check(combatStats.NavesConPiloto==30 && combatStats.NavesSinPiloto==0 && combatStats.PilotosDisponibles==0,"Contadores reflejan 30 con piloto, 0 sin piloto, 0 pilotos libres");
var explorerStats=EstadisticasFlota.Obtener(boarding30).Single(s=>s.Tipo=="Exploradora");
Check(explorerStats.NavesConPiloto==0 && explorerStats.NavesSinPiloto==30 && explorerStats.PilotosDisponibles==30,"Lote de combate no consume naves ni pilotos exploradores");
Check(boarding30.AsignarLote(30,"Exploradora","Negro").Count==0 && boarding30.PilotosDisponibles("Exploradora").Length==30,"Filtro de pelaje sin candidatos no ocupa pilotos");
Check(boarding30.AsignarLote(30,"Exploradora","Atigrado").Count==30,"Lote respeta especialidad y pelaje elegidos");
Check(boarding30.AsignarLote(30,"Combate").Count==0 && boarding30.Flota.Select(n=>n.Piloto!.Identificador).Distinct().Count()==60,"Ningún piloto puede ser asignado nuevamente a otra nave");
Console.WriteLine("TOTAL: " + assertions + " verificaciones correctas.");
