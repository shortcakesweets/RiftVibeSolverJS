const {Hit, SolverData, Solver} = require('./');

const hits = [
  new Hit(0, 10, true),
  new Hit(2, 20, false),
  new Hit(4, 30, true),
  new Hit(6, 40, false)
];

const data = new SolverData(120, 4, [0,2,4,6,8], hits);

const result = Solver.solve(data);
console.log('Total score:', result.totalScore);
console.log('Single vibe activations:', result.singleVibeActivations.length);
console.log('Double vibe activations:', result.doubleVibeActivations.length);
