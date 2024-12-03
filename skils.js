function multiplyMatrices(matrixA, matrixB) {
    let result = [];
    let rowsA = matrixA.length, colsA = matrixA[0].length;
    let rowsB = matrixB.length, colsB = matrixB[0].length;

    if (colsA !== rowsB) {
        throw new Error('Number of columns in matrix A must be equal to number of rows in matrix B');
    }

    for (let i = 0; i < rowsA; i++) {
        result[i] = [];
        for (let j = 0; j < colsB; j++) {
            result[i][j] = 0;
            for (let k = 0; k < colsA; k++) {
                result[i][j] += matrixA[i][k] * matrixB[k][j];
            }
        }
    }

    return result;
}

// Example usage:
const matrixA = [
    [1, 2, 3],
    [4, 5, 6]
];

const matrixB = [
    [7, 8],
    [9, 10],
    [11, 12]
];

console.log(multiplyMatrices(matrixA, matrixB));