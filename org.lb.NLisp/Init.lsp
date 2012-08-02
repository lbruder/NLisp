; vim: et:lisp:ai

; TODO: assoc, string manipulation functions

(define list (lambda (&rest args) args))

(defmacro defun (name args &rest body)
  (list 'define name (cons 'lambda (cons args body))))

(defun not (x) (if x nil t))
(defun <= (a b) (not (> a b)))
(defun >= (a b) (not (< a b)))
(defun caar (x) (car (car x)))
(defun cadr (x) (car (cdr x)))
(defun cdar (x) (cdr (car x)))
(defun cddr (x) (cdr (cdr x)))
(defun caaar (x) (car (car (car x))))
(defun caadr (x) (car (car (cdr x))))
(defun cadar (x) (car (cdr (car x))))
(defun caddr (x) (car (cdr (cdr x))))
(defun cdaar (x) (cdr (car (car x))))
(defun cdadr (x) (cdr (car (cdr x))))
(defun cddar (x) (cdr (cdr (car x))))
(defun cdddr (x) (cdr (cdr (cdr x))))
(defun abs (x) (if (< x 0) (- 0 x) x))
(defun evenp (x) (= 0 (mod x 2)))
(defun oddp (x) (not (evenp x)))

(defmacro cond (&rest forms)
  (defun expand (x)
    (if x
        (list 'if (caar x) (cons 'progn (cdar x)) (expand (cdr x)))
        'nil))
  (expand forms))

(defmacro incf (varname)
  (list 'setq varname (list '+ varname 1)))

(defmacro decf (varname)
  (list 'setq varname (list '- varname 1)))

(defmacro push (item listvar)
  (list 'setq listvar (list 'cons item listvar)))

(defmacro pop (listvar)
  (define sym (gensym))
  (list
    (list 'lambda (list sym)
      (list 'setq listvar (list 'cdr listvar))
      sym)
    (list 'car listvar)))

(defun reverse (lst)
  (define ret nil)
  (while lst
    (push (car lst) ret)
    (setq lst (cdr lst)))
  ret)

(define nreverse reverse) ; TODO

(defun reduce (f lst)
  (define acc (pop lst))
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

(defun identity (x) x)

(defun map (f &rest lists)
  (defun map1 (lst)
    (while lst
      (push (f (car lst)) ret)
      (setq lst (cdr lst))))
  (defun mapn ()
    (while (every identity lists)
      (push (apply f (map car lists)) ret)
      (setq lists (map cdr lists))))
  (define ret nil)
  (if (cdr lists)
    (mapn)
    (map1 (car lists)))
  (nreverse ret))

(defun filter (f lst)
  (define ret nil)
  (while lst
    (if (f (car lst))
        (push (car lst) ret)
        nil)
    (setq lst (cdr lst)))
  (nreverse ret))

(define remove-if-not filter)

(defun remove-if (f lst)
  (filter (lambda (i) (not (f i))) lst))

(defun count-if (f lst)
  (define ret 0)
  (while lst
    (if (f (car lst))
        (incf ret)
        nil)
    (setq lst (cdr lst)))
  ret)

(defun count-if-not (f lst)
  (count-if (lambda (i) (not (f i))) lst))

(defun append (&rest lists)
  (define ret nil)
  (defun append-list (lst)
    (while lst
      (push (car lst) ret)
      (setq lst (cdr lst))))
  (while lists
    (append-list (pop lists)))
  (nreverse ret))

(defmacro let (variable-list &rest body)
  (push (map car variable-list) body)
  (push 'lambda body)
  (append (list body) (map cadr variable-list)))

(defun some (f lst)
  (if lst
      (reduce
        (lambda (acc item) (if acc acc (f item)))
        (cons nil lst))
      nil))

(define all every)
(define any some)

(defun range (&rest args)
  (defun values (from to step)
    (define ret nil)
    (while (< from to)
      (push from ret)
      (setq from (+ from step)))
    (nreverse ret))
  (define arglen (length args))
  (cond ((= 0 arglen) nil)
        ((= 1 arglen) (values 0 (car args) 1))
        (t (values (car args) (cadr args) (if (> arglen 2) (caddr args) 1)))))

(defmacro and (&rest args)
  (defun expand (x)
    (if (cdr x)
        (list 'if (car x) (expand (cdr x)) nil)
        (car x)))
  (expand args))

(defmacro or (&rest args)
  (define sym (gensym))
  (defun expand (x)
    (if (cdr x)
        (list 'progn (list 'setq sym (car x)) (list 'if sym sym (expand (cdr x))))
        (car x)))
  (list (list 'lambda '()
    (list 'define sym 'nil)
    (expand args))))

(defmacro dolist (var-list-form &rest body)
  (let ((var         (car var-list-form))
        (list-form   (cadr var-list-form))
        (result-form (caddr var-list-form))
        (sym         (gensym)))
    (push (list 'setq sym (list 'cdr sym)) body)
    (push (list 'setq var (list 'car sym)) body)
    (push sym body)
    (push 'while body)
    (list 'let (list (list sym list-form)
                     (list var 'nil))
      body
      (list 'setq var 'nil)
      (or result-form 'nil))))

(defmacro dotimes (var-count-form &rest body)
  (let ((var         (car var-count-form))
        (count-form  (cadr var-count-form))
        (result-form (caddr var-count-form))
        (sym         (gensym)))
    (push (list '< var sym) body)
    (push 'while body)
    (list 'let (list (list sym count-form)
                     (list var 0))
      (append body (list (list 'incf var)))
      (or result-form 'nil))))

(defun eql (a b)
  (if (eq a b)
      t
      (and (numberp a)
           (numberp b)
           (= a b))))

(defun lists-equal (a b)
  (define are-equal t)
  (while (and a b are-equal)
    (if (equal (car a) (car b))
      (progn
        (setq a (cdr a))
        (setq b (cdr b)))
      (setq are-equal nil)))
  (and (not a) (not b) are-equal))

(define string= =) ; TODO
(define string> >) ; TODO
(define string< <) ; TODO
(define string>= >=) ; TODO
(define string<= <=) ; TODO

(defun equal (a b)
  (cond ((eql a b) t)
        ((and (consp a) (consp b)) (lists-equal a b))
        ((and (stringp a) (stringp b)) (string= a b))
        (t nil)))

(defmacro when (exp &rest body)
  (list 'if exp (cons 'progn body) nil))

(defmacro unless (exp &rest body)
  (list 'if exp nil (cons 'progn body)))

(defun sort (lst f)
  (when lst
    (define pivot (car lst))
    (append
      (sort (remove-if-not (lambda (x) (f x pivot)) (cdr lst)) f)
      (list pivot)
      (sort (remove-if (lambda (x) (f x pivot)) (cdr lst)) f))))

(defun acons (key datum alist)
  (cons (cons key datum) alist))

(defun pairlis (keys data)
  (map cons keys data))

(defun assoc (item alist)
  (define ret nil)
  (while (and alist (not ret))
    (if (equal (caar alist) item)
        (setq ret (car alist))
        (setq alist (cdr alist))))
  ret)

(map sys:make-symbol-constant (sys:get-global-symbols))
(map sys:make-macro-constant (sys:get-global-macros))
