; TODO:
; cond macro
; range as function based on "while" special form (needs &rest capability OR default parameter values)
; list as function based on &rest capability
; string manipulation functions
; let over lambda (=> optimize other macros)
; equalp, and, or, assoc, ...
; setf macro!
; As soon as (if) TCO is implemented: while as macro, optimizations galore.

(defun not (x) (if x nil t))
(defun <= (a b) (not (> a b)))
(defun >= (a b) (not (< a b)))
(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun abs (x) (if (< x 0) (- 0 x) x))
(defun evenp (x) (= 0 (mod x 2)))
(defun oddp (x) (not (evenp x)))

(defmacro incf (varname)
  (list 'setq varname (list '+ varname 1)))

(defmacro decf (varname)
  (list 'setq varname (list '- varname 1)))

(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (list
    (list 'lambda '(tos)
      (list 'setq listvar (list 'cdr listvar))
      'tos)
    (list 'car listvar)))

(defun map (f lst)
  (define ret nil)
  (while lst
    (push (f (car lst)) ret)
    (setq lst (cdr lst)))
  (nreverse ret))

(defun filter (f lst)
  (define ret nil)
  (while lst
    (if (f (car lst))
      (push (car lst) ret)
      nil)
    (setq lst (cdr lst)))
  (nreverse ret))

(defun reduce (f lst)
  (define acc (car lst))
  (setq lst (cdr lst))
  (if lst
      (while lst
        (setq acc (f acc (car lst)))
        (setq lst (cdr lst)))
      (setq acc nil))
  acc)

(defun every (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc (f item) nil))
        (cons t lst))
      t))

(defun some (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc acc (f item)))
        (cons nil lst))
      nil))
